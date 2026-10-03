import { useState, useEffect } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { formatPrice, formatDateTime } from "../utils/format";
import { usersApi } from "../api";
import TopUpModal from "../components/TopUpModal";
import AuctionCard from "../components/AuctionCard";

export default function ProfilePage() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const [topUpModalOpen, setTopUpModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState("created");

  const [bids, setBids] = useState([]);
  const [wonLots, setWonLots] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      setIsDataLoading(true);
      Promise.all([
        usersApi.getMyBids().catch(() => []),
        usersApi.getMyWonLots().catch(() => []),
      ])
        .then(([bidsData, wonData]) => {
          setBids(bidsData || []);
          setWonLots(wonData || []);
        })
        .finally(() => {
          setIsDataLoading(false);
        });
    }
  }, [isAuthenticated]);

  if (isLoading) {
    return (
      <main className="page-main">
        <div className="catalog-loading">
          <div className="spinner" />
          <p>Loading profile...</p>
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
                {user.role === 1 || user.role === "Admin" ? "Administrator" : "Member"}
              </span>
            </div>
          </div>

          <div className="profile-balance-card">
            <span className="profile-balance-title">Available Balance</span>
            <div className="profile-balance-amount">{formatPrice(user.balance ?? 0)}</div>
            <button
              type="button"
              className="profile-topup-btn"
              onClick={() => setTopUpModalOpen(true)}
            >
              + Top Up Balance
            </button>
          </div>
        </div>

        <div className="profile-tabs">
          <button
            type="button"
            className={`profile-tab ${activeTab === "created" ? "active" : ""}`}
            onClick={() => setActiveTab("created")}
          >
            My Created Lots ({createdLots.length})
          </button>
          <button
            type="button"
            className={`profile-tab ${activeTab === "bids" ? "active" : ""}`}
            onClick={() => setActiveTab("bids")}
          >
            My Bids ({bids.length})
          </button>
          <button
            type="button"
            className={`profile-tab ${activeTab === "won" ? "active" : ""}`}
            onClick={() => setActiveTab("won")}
          >
            Won Auctions ({wonLots.length})
          </button>
        </div>

        <div className="profile-tab-content">
          {isDataLoading && (
            <div className="catalog-loading" style={{ minHeight: "140px" }}>
              <div className="spinner" />
            </div>
          )}

          {!isDataLoading && activeTab === "created" && (
            <div>
              <div className="profile-lots-header">
                <h2>My Listed Auctions</h2>
                <Link to="/create-lot" className="create-lot-link-btn">
                  + Create New Lot
                </Link>
              </div>

              {createdLots.length === 0 ? (
                <div className="profile-empty-state">
                  <p>You haven't created any auctions yet.</p>
                  <Link to="/create-lot" className="btn-primary">
                    List your first item for auction
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

          {!isDataLoading && activeTab === "bids" && (
            <div>
              <div className="profile-lots-header">
                <h2>My Placed Bids</h2>
                <Link to="/" className="create-lot-link-btn">
                  Browse Active Auctions
                </Link>
              </div>

              {bids.length === 0 ? (
                <div className="profile-empty-state">
                  <p>You haven't placed any bids yet.</p>
                  <Link to="/" className="btn-primary">
                    Explore live auctions
                  </Link>
                </div>
              ) : (
                <div className="profile-bids-table-wrapper">
                  <table className="profile-bids-table">
                    <thead>
                      <tr>
                        <th>Item</th>
                        <th>Your Bid</th>
                        <th>Current Price</th>
                        <th>Status</th>
                        <th>Placed At</th>
                        <th>Action</th>
                      </tr>
                    </thead>
                    <tbody>
                      {bids.map((b) => (
                        <tr key={b.id}>
                          <td className="bid-lot-cell">
                            {b.lotMainImageUrl && (
                              <img
                                src={b.lotMainImageUrl}
                                alt={b.lotTitle}
                                className="bid-lot-thumb"
                              />
                            )}
                            <Link to={`/lots/${b.lotId}`} className="bid-lot-link">
                              {b.lotTitle || `Lot #${b.lotId}`}
                            </Link>
                          </td>
                          <td className="bid-amount-cell">
                            <strong>{formatPrice(b.amount)}</strong>
                          </td>
                          <td>{formatPrice(b.lotCurrentPrice)}</td>
                          <td>
                            <span className={`status-pill ${b.lotStatus === 1 ? "active" : b.lotStatus === 2 ? "completed" : "draft"}`}>
                              {b.lotStatus === 1 ? "Active" : b.lotStatus === 2 ? "Completed" : "Ended"}
                            </span>
                          </td>
                          <td className="bid-time-cell">{formatDateTime(b.placedAt)}</td>
                          <td>
                            <Link to={`/lots/${b.lotId}`} className="bid-view-btn">
                              View Lot →
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {!isDataLoading && activeTab === "won" && (
            <div>
              <div className="profile-lots-header">
                <h2>Won Auctions</h2>
              </div>

              {wonLots.length === 0 ? (
                <div className="profile-empty-state">
                  <p>You haven't won any auctions yet.</p>
                  <Link to="/" className="btn-primary">
                    Find items to bid on
                  </Link>
                </div>
              ) : (
                <div className="auction-grid">
                  {wonLots.map((lot) => (
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
