import { useState, useEffect, useCallback } from "react";
import { ethers } from "ethers";
import { Link, useParams } from "react-router-dom";
import LotGallery from "../components/LotGallery";
import { formatDateTime, formatPrice } from "../utils/format.js";
import { LOT_STATUS, getEndLabel, isLotEnded } from "../utils/lot.js";
import { bidsApi, lotsApi } from "../api";
import { useAuth } from "../context/AuthContext";
import AuthModal from "../components/AuthModal";
import TopUpModal from "../components/TopUpModal";
import { getErrorMessage, formatWeb3Error } from "../utils/errors";
import { getAuctionHubConnection, joinLotGroup, leaveLotGroup } from "../services/auctionHub";
import { useWeb3 } from "../context/Web3Context";

function LotDetailsPage() {
  const { id } = useParams();
  const lotId = Number(id);

  const { user, isAuthenticated, refreshUser } = useAuth();
  const {
    account: web3Account,
    balance: web3Balance,
    chainId,
    connectWallet,
    switchToGanache,
    placeOnChainBid,
    getPendingReturn,
    getMinimumBid,
    withdrawOutbidBid,
  } = useWeb3();

  const [lot, setLot] = useState(null);
  const [bids, setBids] = useState([]);
  const [amount, setAmount] = useState("");
  const [bidMode, setBidMode] = useState("usd"); // "usd" | "eth"
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isClosing, setIsClosing] = useState(false);
  const [pricePulse, setPricePulse] = useState(false);

  const [ethBidAmount, setEthBidAmount] = useState("0.05");
  const [isOnChainBidding, setIsOnChainBidding] = useState(false);
  const [onChainSuccess, setOnChainSuccess] = useState("");
  const [pendingReturn, setPendingReturn] = useState(0n);
  const [minimumEthBid, setMinimumEthBid] = useState(null);
  const [isWithdrawing, setIsWithdrawing] = useState(false);

  const [authModalOpen, setAuthModalOpen] = useState(false);
  const [topUpModalOpen, setTopUpModalOpen] = useState(false);

  const fetchLotData = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const [lotData, bidsData] = await Promise.all([
        lotsApi.getById(lotId),
        bidsApi.getByLotId(lotId).catch(() => ({ items: [] })),
      ]);
      setLot(lotData);
      setBids(bidsData.items || []);
      const nextMin = Number(lotData.currentPrice ?? 0) + Number(lotData.minBidStep ?? 0);
      setAmount(String(nextMin));
    } catch (err) {
      console.error("Failed to load lot", err);
      setError("Failed to load lot details. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }, [lotId]);

  useEffect(() => {
    fetchLotData();
  }, [fetchLotData]);

  // SignalR real-time updates for bids
  useEffect(() => {
    if (!lotId || isNaN(lotId)) return;

    let isMounted = true;
    joinLotGroup(lotId);

    const hub = getAuctionHubConnection();
    const handleReceiveBid = (newBid) => {
      if (!isMounted) return;
      setBids((prev) => {
        if (prev.some((b) => b.id === newBid.id)) return prev;
        return [newBid, ...prev];
      });
      setLot((prev) => {
        if (!prev) return prev;
        return {
          ...prev,
          currentPrice: newBid.amount,
          currentPriceEth: newBid.currency === 1 ? newBid.amountEth : null,
          winnerId: newBid.userId,
          winner: {
            id: newBid.userId,
            userName: newBid.userName || `User #${newBid.userId}`,
          },
        };
      });
      setPricePulse(true);
      setTimeout(() => setPricePulse(false), 1200);
    };

    hub.on("ReceiveBid", handleReceiveBid);

    return () => {
      isMounted = false;
      hub.off("ReceiveBid", handleReceiveBid);
      leaveLotGroup(lotId);
    };
  }, [lotId]);

  useEffect(() => {
    let active = true;
    if (!lot?.contractAddress || !web3Account) {
      setPendingReturn(0n);
      return undefined;
    }
    getPendingReturn(lot.contractAddress, web3Account)
      .then((amount) => { if (active) setPendingReturn(amount); })
      .catch(() => { if (active) setPendingReturn(0n); });
    return () => { active = false; };
  }, [lot?.contractAddress, web3Account, bids, getPendingReturn]);

  useEffect(() => {
    let active = true;
    if (!lot?.contractAddress || !web3Account) {
      setMinimumEthBid(null);
      return undefined;
    }
    getMinimumBid(lot.contractAddress)
      .then((minimum) => {
        if (!active || minimum === null) return;
        setMinimumEthBid(minimum);
        const suggestion = Math.ceil((Number(ethers.formatEther(minimum)) + 0.0001) * 10000) / 10000;
        setEthBidAmount(suggestion.toFixed(4));
      })
      .catch(() => { if (active) setMinimumEthBid(null); });
    return () => { active = false; };
  }, [lot?.contractAddress, web3Account, bids, getMinimumBid]);

  const handleWithdraw = async () => {
    setIsWithdrawing(true);
    setError("");
    try {
      await withdrawOutbidBid(lot.contractAddress);
      setPendingReturn(0n);
      setSuccessMsg("Your outbid ETH was returned to your wallet.");
    } catch (err) {
      setError(formatWeb3Error(err));
    } finally {
      setIsWithdrawing(false);
    }
  };

  if (isLoading) {
    return (
      <main className="page-main">
        <div className="catalog-loading">
          <div className="spinner" />
          <p>Loading lot details...</p>
        </div>
      </main>
    );
  }

  if (!lot) {
    return (
      <main className="not-found">
        <span className="not-found__code">404</span>
        <h1>Lot not found</h1>
        <p>The lot with this ID does not exist or has been removed.</p>
        <Link to="/">Back to catalog</Link>
      </main>
    );
  }

  const minBid = Number(lot.currentPrice ?? 0) + Number(lot.minBidStep ?? 0);
  const isOwner = user && lot.seller && lot.seller.id === user.id;
  const ended = isLotEnded(lot);
  const canClose = isOwner && lot.status === LOT_STATUS.ACTIVE && ended;

  const handlePlaceBid = async (e) => {
    e.preventDefault();
    setError("");
    setSuccessMsg("");

    if (!isAuthenticated) {
      setAuthModalOpen(true);
      return;
    }

    const value = Number(amount);
    if (!Number.isFinite(value) || value < minBid) {
      setError(`Minimum bid must be at least ${formatPrice(minBid)}`);
      return;
    }

    if (user.balance < value) {
      setError(`Insufficient balance (${formatPrice(user.balance)}). Please top up your balance.`);
      return;
    }

    setIsSubmitting(true);
    try {
      await bidsApi.create(lot.id, value);
      setSuccessMsg(`Your bid of ${formatPrice(value)} was placed successfully!`);
      await fetchLotData();
      await refreshUser();
    } catch (err) {
      setError(getErrorMessage(err, "Failed to place bid."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOnChainBid = async (e) => {
    e.preventDefault();
    setError("");
    setOnChainSuccess("");
    setSuccessMsg("");

    if (!isAuthenticated) {
      setAuthModalOpen(true);
      return;
    }

    if (!web3Account) {
      try {
        await connectWallet();
      } catch (err) {
        setError(formatWeb3Error(err));
      }
      return;
    }

    const val = parseFloat(ethBidAmount);
    if (!val || val <= 0) {
      setError("Please enter a valid ETH amount (e.g. 0.05).");
      return;
    }

    setIsOnChainBidding(true);
    try {
      // Step 1: send tx to smart contract via MetaMask
      const result = await placeOnChainBid(val, lot.id, lot.contractAddress);

      // Step 2: register the confirmed tx on the platform backend
      await bidsApi.createOnChain({
        lotId: lot.id,
        txHash: result.txHash,
        amountEth: val,
        walletAddress: web3Account,
      });

      const shortHash = result.txHash
        ? `${result.txHash.slice(0, 10)}...${result.txHash.slice(-8)}`
        : "";
      setOnChainSuccess(`ETH bid recorded! Tx: ${shortHash}`);

      // Step 3: refresh UI
      await fetchLotData();
      await refreshUser();
    } catch (err) {
      // formatWeb3Error handles MetaMask rejections; getErrorMessage handles API errors
      const msg = err?.response
        ? getErrorMessage(err, "Failed to register ETH bid.")
        : formatWeb3Error(err);
      setError(msg);
    } finally {
      setIsOnChainBidding(false);
    }
  };

  const handleCloseLot = async () => {
    if (!window.confirm("Are you sure you want to close this auction and finalize the result?")) {
      return;
    }
    setIsClosing(true);
    setError("");
    try {
      await lotsApi.close(lot.id);
      setSuccessMsg("Auction has been successfully completed!");
      await fetchLotData();
      await refreshUser();
    } catch (err) {
      setError(getErrorMessage(err, "Failed to close lot."));
    } finally {
      setIsClosing(false);
    }
  };

  return (
    <main className="page-main">
      <Link className="lot-back" to="/">
        ← Back to auctions
      </Link>

      <section className="lot-details">
        <LotGallery lot={lot} />

        <div className="bid-panel">
          {lot.category && <span className="bid-panel__category">{lot.category.name}</span>}
          <h1>{lot.title}</h1>

          <div className="bid-panel__seller">
            Seller: <strong>{lot.seller?.userName || "Unknown"}</strong>
          </div>

          <div className="bid-panel__price-label" style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <span>Current price</span>
            {lot.status === LOT_STATUS.ACTIVE && !ended && (
              <span className="live-badge">
                <span className="live-badge__dot" /> Live
              </span>
            )}
          </div>
          <div className={`bid-panel__price ${pricePulse ? "price-pulse" : ""}`}>
            {formatPrice(lot.currentPrice)}
          </div>
          <div className="bid-panel__end">{getEndLabel(lot)}</div>

          {lot.winner && (
            <div className={`bid-panel__winner ${lot.status === LOT_STATUS.COMPLETED || ended ? "completed" : "active-leader"}`}>
              {lot.status === LOT_STATUS.COMPLETED || ended ? (
                <>🏆 Winner: <strong>{lot.winner.userName || `User #${lot.winner.id}`}</strong></>
              ) : (
                <>🥇 Top Bidder: <strong>{lot.winner.userName || `User #${lot.winner.id}`}</strong></>
              )}
            </div>
          )}

          {error && <div className="bid-panel__alert error" role="alert">{error}</div>}
          {successMsg && <div className="bid-panel__alert success" role="status">{successMsg}</div>}
          {onChainSuccess && <div className="bid-panel__alert success" role="status">{onChainSuccess}</div>}

          {pendingReturn > 0n && (
            <button type="button" className="quick-bid-preset-btn"
              onClick={handleWithdraw} disabled={isWithdrawing}>
              {isWithdrawing ? "Withdrawing..." : `Withdraw ${ethers.formatEther(pendingReturn)} ETH from an outbid bid`}
            </button>
          )}

          {canClose && (
            <div className="owner-close-box">
              <p>Auction duration has ended. You can finalize results and receive funds.</p>
              <button
                type="button"
                className="close-lot-btn"
                onClick={handleCloseLot}
                disabled={isClosing}
              >
                {isClosing ? "Closing..." : "Close Auction & Collect Funds"}
              </button>
            </div>
          )}

          {lot.status === LOT_STATUS.ACTIVE && !ended ? (
            isOwner ? (
              <p className="bid-panel__owner-note">
                ℹ️ You are the seller of this lot. You cannot bid on your own item.
              </p>
            ) : (
              <>
                <div className="bid-mode-toggle" role="tablist" aria-label="Bidding Currency Mode">
                  <button
                    type="button"
                    role="tab"
                    aria-selected={bidMode === "usd"}
                    className={`bid-mode-tab ${bidMode === "usd" ? "active" : ""}`}
                    onClick={() => {
                      setBidMode("usd");
                      setError("");
                      setOnChainSuccess("");
                    }}
                  >
                    <span className="bid-mode-tab__icon">💵</span>
                    <span className="bid-mode-tab__text">USD ($)</span>
                  </button>
                  <button
                    type="button"
                    role="tab"
                    aria-selected={bidMode === "eth"}
                    className={`bid-mode-tab ${bidMode === "eth" ? "active" : ""}`}
                    onClick={() => {
                      setBidMode("eth");
                      setError("");
                      setSuccessMsg("");
                    }}
                  >
                    <span className="bid-mode-tab__icon">🦊</span>
                    <span className="bid-mode-tab__text">Ethereum (ETH)</span>
                  </button>
                </div>

                <p className="field-hint">USD and ETH bids compete for the same lot and winner. ETH amounts are converted at the demo exchange rate.</p>

                {bidMode === "usd" ? (
                  <form onSubmit={handlePlaceBid} className="bid-form">
                    <div className="bid-form-header">
                      <label htmlFor="bid-amount">Your bid (min {formatPrice(minBid)})</label>
                      {isAuthenticated && (
                        <span className="user-balance-hint">
                          Balance: {formatPrice(user?.balance ?? 0)}
                        </span>
                      )}
                    </div>

                    <div className="bid-form-vertical">
                      <input
                        id="bid-amount"
                        className="bid-full-input"
                        type="number"
                        min={minBid}
                        step={lot.minBidStep || "0.01"}
                        value={amount}
                        disabled={!isAuthenticated}
                        onChange={(event) => {
                          setAmount(event.target.value);
                          setError("");
                        }}
                        onBlur={() => {
                          const num = parseFloat(amount);
                          if (Number.isFinite(num) && num > 0) {
                            setAmount(num.toFixed(2));
                          }
                        }}
                        required
                      />

                      {isAuthenticated && (
                        <div className="quick-bid-presets">
                          {[1, 2, 5, 10].map((multiplier) => {
                            const step = Number(lot.minBidStep) || 1;
                            const increment = step * multiplier;
                            return (
                              <button
                                key={multiplier}
                                type="button"
                                className="quick-bid-preset-btn"
                                onClick={() => {
                                  const currentVal = parseFloat(amount);
                                  const base = Number.isFinite(currentVal) && currentVal >= minBid
                                    ? currentVal
                                    : Number(lot.currentPrice);
                                  const nextVal = base + increment;
                                  setAmount(nextVal.toFixed(2));
                                  setError("");
                                }}
                              >
                                +{formatPrice(increment)}
                              </button>
                            );
                          })}
                        </div>
                      )}

                      <button
                        type="submit"
                        className="bid-main-button"
                        disabled={isSubmitting}
                      >
                        {isSubmitting
                          ? "Placing bid..."
                          : !isAuthenticated
                          ? "Sign in to place bid"
                          : "Place Bid"}
                      </button>
                    </div>
                  </form>
                ) : (
                  <form onSubmit={handleOnChainBid} className="bid-form">
                    <div className="bid-form-header">
                      <label htmlFor="eth-bid-amount">Your bid in ETH</label>
                      {web3Account ? (
                        <span className="user-balance-hint">
                          Wallet: {web3Balance !== null ? `${web3Balance} ETH` : "Connected"}
                        </span>
                      ) : (
                        <button
                          type="button"
                          className="connect-wallet-link"
                          onClick={connectWallet}
                        >
                          🦊 Connect MetaMask
                        </button>
                      )}
                    </div>
                    {minimumEthBid !== null && (
                      <p className="field-hint">ETH bid must exceed {ethers.formatEther(minimumEthBid)} ETH to beat the current USD or ETH bid.</p>
                    )}
                    {lot.contractAddress && web3Account && minimumEthBid === null && (
                      <p className="bid-panel__alert warning" role="status">
                        The auction contract is unavailable or uses an older version. ETH bidding is paused for this lot.
                      </p>
                    )}

                    {web3Account && chainId && chainId !== 1337 && chainId !== 5777 && (
                      <div className="bid-panel__alert warning" style={{ marginBottom: 12, display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: 8 }}>
                        <span>⚠️ You are connected to Chain #{chainId}. Your 100 ETH is on Ganache (Chain 1337).</span>
                        <button
                          type="button"
                          className="quick-bid-preset-btn"
                          style={{ background: "#fef3c7", borderColor: "#fcd34d", color: "#92400e" }}
                          onClick={switchToGanache}
                        >
                          ⚡ Switch to Ganache
                        </button>
                      </div>
                    )}

                    {lot.contractAddress && (
                      <div className="eth-contract-info">
                        <span className="eth-contract-badge">Smart Contract</span>
                        <span className="eth-contract-addr" title={lot.contractAddress}>
                          {lot.contractAddress.slice(0, 8)}...{lot.contractAddress.slice(-6)}
                        </span>
                        <span className="eth-network-badge">
                          {chainId === 1337 || chainId === 5777 ? `Ganache (${chainId})` : chainId === 31337 ? "Hardhat (31337)" : `Chain ID: ${chainId || "Unknown"}`}
                        </span>
                      </div>
                    )}

                    {!lot.contractAddress && (
                      <div className="bid-panel__alert warning" role="status">
                        ETH bidding is being prepared for this lot. Please try again shortly.
                      </div>
                    )}

                    <div className="bid-form-vertical">
                      <input
                        id="eth-bid-amount"
                        className="bid-full-input"
                        type="number"
                        min="0.0001"
                        step="0.0001"
                        placeholder="0.05"
                        value={ethBidAmount}
                        disabled={isOnChainBidding}
                        onChange={(event) => {
                          setEthBidAmount(event.target.value);
                          setError("");
                        }}
                        onBlur={() => {
                          const num = parseFloat(ethBidAmount);
                          if (Number.isFinite(num) && num > 0) {
                            setEthBidAmount(num.toFixed(4));
                          }
                        }}
                        required
                      />

                      <div className="quick-bid-presets">
                        {[0.01, 0.05, 0.1, 0.5].map((increment) => (
                          <button
                            key={increment}
                            type="button"
                            className="quick-bid-preset-btn"
                            disabled={isOnChainBidding}
                            onClick={() => {
                              const currentVal = parseFloat(ethBidAmount);
                              const base = Number.isFinite(currentVal) && currentVal > 0 ? currentVal : 0;
                              const nextVal = base + increment;
                              setEthBidAmount(nextVal.toFixed(4));
                              setError("");
                            }}
                          >
                            +{increment} ETH
                          </button>
                        ))}
                      </div>

                      {!web3Account ? (
                        <button
                          type="button"
                          className="bid-main-button eth-mode"
                          onClick={connectWallet}
                          disabled={isOnChainBidding}
                        >
                          🦊 Connect MetaMask to Bid in ETH
                        </button>
                      ) : (
                        <button
                          type="submit"
                          className="bid-main-button eth-mode"
                          disabled={isOnChainBidding || !lot.contractAddress || minimumEthBid === null}
                        >
                          {isOnChainBidding ? "Confirming in MetaMask..." : lot.contractAddress ? "Place ETH Bid" : "ETH bidding unavailable"}
                        </button>
                      )}
                    </div>
                  </form>
                )}
              </>
            )
          ) : (
            <div className="bid-panel__closed-note">
              🔒 Bidding is closed for this lot.
            </div>
          )}
        </div>
      </section>

      {lot.description && (
        <section className="lot-description-section">
          <h2>Description</h2>
          <p className="lot-description">{lot.description}</p>
        </section>
      )}

      <section className="lot-bids-section">
        <h2>Bid History ({bids.length})</h2>
        {bids.length === 0 ? (
          <p className="no-bids-message">No bids placed yet. Be the first to bid!</p>
        ) : (
          <div className="bids-table-wrapper">
            <table className="bids-table">
              <thead>
                <tr>
                  <th>Bidder</th>
                  <th>Bid Amount</th>
                  <th>Time</th>
                </tr>
              </thead>
              <tbody>
                {bids.map((b) => (
                  <tr key={b.id}>
                    <td>{b.userName || `User #${b.userId}`}</td>
                    <td className="bid-amount-cell">
                      {b.currency === 1 ? (
                        // ETH bid
                        <span className="bid-currency-eth">
                          <span className="bid-badge bid-badge--eth">🦊 ETH</span>
                          {b.amountEth != null
                            ? `${Number(b.amountEth).toFixed(4)} ETH`
                            : formatPrice(b.amount)}
                          <span> ({formatPrice(b.amount)} equivalent)</span>
                          {b.txHash && (
                            <a
                              className="bid-tx-link"
                              href={`#tx-${b.txHash.slice(0, 8)}`}
                              title={b.txHash}
                              onClick={(e) => {
                                e.preventDefault();
                                navigator.clipboard.writeText(b.txHash);
                              }}
                            >
                              {b.txHash.slice(0, 8)}…{b.txHash.slice(-6)}
                            </a>
                          )}
                        </span>
                      ) : (
                        // USD bid
                        <span className="bid-currency-usd">
                          <span className="bid-badge bid-badge--usd">💵 USD</span>
                          {formatPrice(b.amount)}
                        </span>
                      )}
                    </td>
                    <td>{formatDateTime(b.placedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <AuthModal
        isOpen={authModalOpen}
        onClose={() => setAuthModalOpen(false)}
      />

      <TopUpModal
        isOpen={topUpModalOpen}
        onClose={() => setTopUpModalOpen(false)}
      />
    </main>
  );
}

export default LotDetailsPage;
