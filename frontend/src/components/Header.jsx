import { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { formatPrice } from "../utils/format";
import AuthModal from "./AuthModal";
import TopUpModal from "./TopUpModal";

function Header() {
  const { pathname } = useLocation();
  const { user, isAuthenticated, logout } = useAuth();

  const [authModalOpen, setAuthModalOpen] = useState(false);
  const [authModalMode, setAuthModalMode] = useState("login");
  const [topUpModalOpen, setTopUpModalOpen] = useState(false);

  const openAuth = (mode) => {
    setAuthModalMode(mode);
    setAuthModalOpen(true);
  };

  return (
    <>
      <div className="announcement">
        Live Auctions • Instant Bidding • Verified Sellers
      </div>

      <header className="site-header">
        <div className="site-header__inner">
          <Link className="brand" to="/" aria-label="Bestauction home">
            <span className="brand-mark">+</span>
            <span>bestauction</span>
          </Link>

          <nav className="main-nav" aria-label="Main navigation">
            <Link to="/" aria-current={pathname === "/" ? "page" : undefined}>
              Auctions
            </Link>
            {isAuthenticated && (
              <>
                <Link
                  to="/create-lot"
                  aria-current={pathname === "/create-lot" ? "page" : undefined}
                >
                  Create Lot
                </Link>
                <Link
                  to="/profile"
                  aria-current={pathname === "/profile" ? "page" : undefined}
                >
                  My Profile
                </Link>
              </>
            )}
          </nav>

          <div className="header-actions">
            {isAuthenticated ? (
              <div className="header-user-badge">
                <button
                  type="button"
                  className="balance-pill"
                  onClick={() => setTopUpModalOpen(true)}
                  title="Top up balance"
                >
                  <span className="balance-label">Balance:</span>
                  <span className="balance-value">{formatPrice(user?.balance ?? 0)}</span>
                  <span className="balance-add" aria-hidden="true">+</span>
                </button>

                <Link to="/profile" className="user-name-link" title="Open profile">
                  👤 {user?.userName}
                </Link>

                <button
                  type="button"
                  className="header-logout-btn"
                  onClick={logout}
                  title="Sign out"
                >
                  Sign Out
                </button>
              </div>
            ) : (
              <div className="header-auth-buttons">
                <button
                  type="button"
                  className="header-login-btn"
                  onClick={() => openAuth("login")}
                >
                  Sign In
                </button>
                <button
                  type="button"
                  className="header-cta"
                  onClick={() => openAuth("register")}
                >
                  Register
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      <AuthModal
        isOpen={authModalOpen}
        onClose={() => setAuthModalOpen(false)}
        initialMode={authModalMode}
      />

      <TopUpModal
        isOpen={topUpModalOpen}
        onClose={() => setTopUpModalOpen(false)}
      />
    </>
  );
}

export default Header;
