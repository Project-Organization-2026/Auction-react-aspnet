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
      // Suggest next minimum bid
      const nextMin = Number(lotData.currentPrice ?? 0) + Number(lotData.minBidStep ?? 0);
      setAmount(String(nextMin));
    } catch (err) {
      console.error("Помилка завантаження лота", err);
      setError("Не вдалося завантажити інформацію про лот.");
    } finally {
      setIsLoading(false);
    }
  }, [lotId]);

  useEffect(() => {
    fetchLotData();
  }, [fetchLotData]);

  if (isLoading) {
    return (
      <main className="page-main">
        <div className="catalog-loading">
          <div className="spinner" />
          <p>Завантаження інформації про лот...</p>
        </div>
      </main>
    );
  }

  if (!lot) {
    return (
      <main className="not-found">
        <span className="not-found__code">404</span>
        <h1>Лот не знайдено</h1>
        <p>Лот із вказаним ID не існує або був видалений.</p>
        <Link to="/">Повернутися до каталогу</Link>
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
      setError(`Мінімальна ставка повинна бути не менше ${formatPrice(minBid)}`);
      return;
    }

    if (user.balance < value) {
      setError(`Недостатньо коштів на балансі (${formatPrice(user.balance)}). Поповніть баланс.`);
      return;
    }

    setIsSubmitting(true);
    try {
      await bidsApi.create(lot.id, value);
      setSuccessMsg(`Ставку ${formatPrice(value)} успішно прийнято!`);
      await fetchLotData();
      await refreshUser();
    } catch (err) {
      setError(getErrorMessage(err, "Не вдалося зробити ставку."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCloseLot = async () => {
    if (!window.confirm("Ви дійсно бажаєте закрити цей аукціон та зафіксувати результат?")) {
      return;
    }
    setIsClosing(true);
    setError("");
    try {
      await lotsApi.close(lot.id);
      setSuccessMsg("Аукціон успішно завершено!");
      await fetchLotData();
      await refreshUser();
    } catch (err) {
      setError(getErrorMessage(err, "Помилка закриття лота."));
    } finally {
      setIsClosing(false);
    }
  };

  return (
    <main className="page-main">
      <Link className="lot-back" to="/">
        ← Повернутися до аукціонів
      </Link>

      <section className="lot-details">
        <LotGallery lot={lot} />

        <div className="bid-panel">
          {lot.category && <span className="bid-panel__category">{lot.category.name}</span>}
          <h1>{lot.title}</h1>

          <div className="bid-panel__seller">
            Продавець: <strong>{lot.seller?.userName || "Невідомий"}</strong>
          </div>

          <div className="bid-panel__price-label">Поточна ціна</div>
          <div className="bid-panel__price">{formatPrice(lot.currentPrice)}</div>
          <div className="bid-panel__end">{getEndLabel(lot)}</div>

          {lot.winner && (
            <div className="bid-panel__winner">
              🏆 Переможець: <strong>{lot.winner.userName || `ID: ${lot.winner.id}`}</strong>
            </div>
          )}

          {error && <div className="bid-panel__alert error" role="alert">{error}</div>}
          {successMsg && <div className="bid-panel__alert success" role="status">{successMsg}</div>}

          {canClose && (
            <div className="owner-close-box">
              <p>Час аукціону минув. Ви можете зафіксувати результати.</p>
              <button
                type="button"
                className="close-lot-btn"
                onClick={handleCloseLot}
                disabled={isClosing}
              >
                {isClosing ? "Завершення..." : "Завершити аукціон та отримати кошти"}
              </button>
            </div>
          )}

          {lot.status === LOT_STATUS.ACTIVE && !ended ? (
            isOwner ? (
              <p className="bid-panel__owner-note">
                ℹ️ Ви є продавцем цього лота. Ви не можете робити ставки на власний товар.
              </p>
            ) : (
              <form onSubmit={handlePlaceBid} className="bid-form">
                <div className="bid-form-header">
                  <label htmlFor="bid-amount">Ваша ставка (мін. {formatPrice(minBid)})</label>
                  {isAuthenticated && (
                    <span className="user-balance-hint">
                      Баланс: {formatPrice(user?.balance ?? 0)}{" "}
                      <button
                        type="button"
                        className="quick-topup-link"
                        onClick={() => setTopUpModalOpen(true)}
                      >
                        + Поповнити
                      </button>
                    </span>
                  )}
                </div>

                <div className="bid-input-group">
                  <input
                    id="bid-amount"
                    type="number"
                    min={minBid}
                    step={lot.minBidStep || "0.01"}
                    value={amount}
                    onChange={(event) => {
                      setAmount(event.target.value);
                      setError("");
                    }}
                    required
                  />
                  <button type="submit" disabled={isSubmitting}>
                    {isSubmitting
                      ? "Відправка..."
                      : !isAuthenticated
                      ? "Увійти і зробити ставку"
                      : "Зробити ставку"}
                  </button>
                </div>
              </form>
            )
          ) : (
            <div className="bid-panel__closed-note">
              🔒 Торги по цьому лоту завершено.
            </div>
          )}
        </div>
      </section>

      {lot.description && (
        <section className="lot-description-section">
          <h2>Опис товару</h2>
          <p className="lot-description">{lot.description}</p>
        </section>
      )}

      <section className="lot-bids-section">
        <h2>Історія ставок ({bids.length})</h2>
        {bids.length === 0 ? (
          <p className="no-bids-message">Ставок поки що немає. Будьте першим!</p>
        ) : (
          <div className="bids-table-wrapper">
            <table className="bids-table">
              <thead>
                <tr>
                  <th>Учасник</th>
                  <th>Ставка</th>
                  <th>Час</th>
                </tr>
              </thead>
              <tbody>
                {bids.map((b) => (
                  <tr key={b.id}>
                    <td>{b.userName || `Користувач #${b.userId}`}</td>
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
