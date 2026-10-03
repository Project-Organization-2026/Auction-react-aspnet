import { useState, useRef, useEffect } from "react";
import { useWeb3 } from "../context/Web3Context";

export default function WalletButton() {
  const {
    account,
    balance,
    chainId,
    isConnected,
    isConnecting,
    error,
    connectWallet,
    disconnectWallet,
    switchToGanache,
  } = useWeb3();

  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [copied, setCopied] = useState(false);
  const dropdownRef = useRef(null);

  const shortenAddress = (addr) => {
    if (!addr) return "";
    return `${addr.substring(0, 6)}...${addr.substring(addr.length - 4)}`;
  };

  const handleCopy = () => {
    if (account) {
      navigator.clipboard.writeText(account);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setDropdownOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (!isConnected) {
    return (
      <div className="wallet-btn-wrap">
        <button
          type="button"
          className="header-wallet-btn"
          onClick={connectWallet}
          disabled={isConnecting}
          title={error || "Connect MetaMask or Web3 Wallet"}
        >
          <span className="wallet-icon">🦊</span>
          <span>{isConnecting ? "Connecting..." : "Connect Wallet"}</span>
        </button>
        {error && <span className="wallet-error-tip">{error}</span>}
      </div>
    );
  }

  const isGanache = chainId === 1337 || chainId === 5777;

  return (
    <div className="wallet-connected-wrap" ref={dropdownRef}>
      <button
        type="button"
        className="header-wallet-pill"
        onClick={() => setDropdownOpen(!dropdownOpen)}
        title="View wallet details"
      >
        <span className="wallet-dot" />
        <span className="wallet-addr">{shortenAddress(account)}</span>
        {balance !== null && <span className="wallet-eth-bal">{balance} ETH</span>}
      </button>

      {dropdownOpen && (
        <div className="wallet-dropdown-menu">
          <div className="wallet-dropdown-header">
            <span className="wallet-dropdown-title">Web3 Wallet</span>
            <span className={`wallet-network-tag ${isGanache ? "active" : "warning"}`}>
              {isGanache ? "Ganache (1337)" : chainId === 31337 ? "Hardhat" : `Chain: ${chainId}`}
            </span>
          </div>

          <div className="wallet-dropdown-body">
            <div className="wallet-addr-full">{account}</div>
            <div className="wallet-bal-detail">
              Balance: <strong>{balance ?? "0.00"} ETH</strong>
            </div>
            {!isGanache && (
              <div style={{ marginTop: 10 }}>
                <div style={{ fontSize: 11, color: "#d97706", marginBottom: 6 }}>
                  ⚠️ Connected to wrong network. Your 100 ETH is on Ganache.
                </div>
                <button
                  type="button"
                  className="wallet-action-btn"
                  style={{ width: "100%", background: "#fef3c7", borderColor: "#fcd34d", color: "#92400e" }}
                  onClick={async () => {
                    await switchToGanache();
                    setDropdownOpen(false);
                  }}
                >
                  ⚡ Switch to Ganache (1337)
                </button>
              </div>
            )}
          </div>

          <div className="wallet-dropdown-actions">
            <button
              type="button"
              className="wallet-action-btn"
              onClick={handleCopy}
            >
              {copied ? "✓ Copied!" : "📋 Copy Address"}
            </button>
            <button
              type="button"
              className="wallet-action-btn disconnect"
              onClick={() => {
                disconnectWallet();
                setDropdownOpen(false);
              }}
            >
              Disconnect
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
