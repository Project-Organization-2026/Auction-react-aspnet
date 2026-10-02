import { useState, useEffect, useCallback } from "react";
import { Link, useParams } from "react-router-dom";
import LotGallery from "../components/LotGallery";
import { formatDateTime, formatPrice } from "../utils/format.js";
import { LOT_STATUS, getEndLabel, isLotEnded } from "../utils/lot.js";
import { bidsApi, lotsApi } from "../api";
import { useAuth } from "../context/AuthContext";
import AuthModal from "../components/AuthModal";
import TopUpModal from "../components/TopUpModal";
import { getErrorMessage } from "../utils/errors";
import { getAuctionHubConnection, joinLotGroup, leaveLotGroup } from "../services/auctionHub";

function LotDetailsPage() {
  const { id } = useParams();
  const lotId = Number(id);

  const { user, isAuthenticated, refreshUser } = useAuth();

  const [lot, setLot] = useState(null);
  const [bids, setBids] = useState([]);
  const [amount, setAmount] = useState("");
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isClosing, setIsClosing] = useState(false);
  const [pricePulse, setPricePulse] = useState(false);

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
                    <td className="bid-amount-cell">{formatPrice(b.amount)}</td>
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
