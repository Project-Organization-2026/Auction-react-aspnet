import { useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { formatPrice } from "../utils/format";
import TopUpModal from "../components/TopUpModal";
import AuctionCard from "../components/AuctionCard";

export default function ProfilePage() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const [topUpModalOpen, setTopUpModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState("created");

  if (isLoading) {
    return (
      <main className="page-main">
        <div className="catalog-loading">
          <div className="spinner" />
          <p>Завантаження профілю...</p>
        </div>
      </main>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const createdLots = user?.createdLots || [];

  return (
    <main className="page-main">
      <section className="profile-section">
        <div className="profile-header">
          <div className="profile-user-info">
            <div className="profile-avatar">
              {user.userName ? user.userName.charAt(0).toUpperCase() : "U"}
            </div>
            <div>
              <h1>{user.userName}</h1>
              <p className="profile-email">{user.email}</p>
              <span className="profile-role-badge">
                {user.role === 1 || user.role === "Admin" ? "Адміністратор" : "Користувач"}
              </span>
            </div>
          </div>

          <div className="profile-balance-card">
            <span className="profile-balance-title">Поточний баланс</span>
            <div className="profile-balance-amount">{formatPrice(user.balance ?? 0)}</div>
            <button
              type="button"
              className="profile-topup-btn"
              onClick={() => setTopUpModalOpen(true)}
            >
              + Поповнити баланс
            </button>
          </div>
        </div>

        <div className="profile-tabs">
          <button
            type="button"
            className={`profile-tab ${activeTab === "created" ? "active" : ""}`}
            onClick={() => setActiveTab("created")}
          >
            Створені лоти ({createdLots.length})
          </button>
        </div>

        <div className="profile-tab-content">
          {activeTab === "created" && (
            <div>
              <div className="profile-lots-header">
                <h2>Мої виставлені лоти</h2>
                <Link to="/create-lot" className="create-lot-link-btn">
                  + Створити новий лот
                </Link>
              </div>

              {createdLots.length === 0 ? (
                <div className="profile-empty-state">
                  <p>Ви ще не створили жодного лота.</p>
                  <Link to="/create-lot" className="btn-primary">
                    Виставити перший товар на аукціон
                  </Link>
                </div>
              ) : (
                <div className="auction-grid">
                  {createdLots.map((lot) => (
                    <AuctionCard key={lot.id} lot={lot} />
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </section>

      <TopUpModal
        isOpen={topUpModalOpen}
        onClose={() => setTopUpModalOpen(false)}
      />
    </main>
  );
}
