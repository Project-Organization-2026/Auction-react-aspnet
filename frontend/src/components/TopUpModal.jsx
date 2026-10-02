import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { formatPrice } from "../utils/format";
import { getErrorMessage } from "../utils/errors";

export default function TopUpModal({ isOpen, onClose }) {
  const [amount, setAmount] = useState("100");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const { topUpBalance, user } = useAuth();

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    const val = Number(amount);
    if (!Number.isFinite(val) || val <= 0) {
      setError("Сума має бути більшою за 0");
      return;
    }

    setIsSubmitting(true);
    setError("");

    try {
      await topUpBalance(val);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Помилка поповнення балансу."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Поповнення балансу</h2>
          <button className="modal-close" onClick={onClose} aria-label="Закрити">
            ✕
          </button>
        </div>

        <p className="modal-sub">
          Поточний баланс: <strong>{formatPrice(user?.balance ?? 0)}</strong>
        </p>

        <form className="modal-form" onSubmit={handleSubmit}>
          {error && <div className="modal-error" role="alert">{error}</div>}

          <div className="form-group">
            <label htmlFor="topup-amount">Сума ($)</label>
            <input
              id="topup-amount"
              type="number"
              min="1"
              step="0.01"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              required
            />
          </div>

          <div className="quick-amounts">
            {[50, 100, 250, 500].map((val) => (
              <button
                type="button"
                key={val}
                className="quick-amount-btn"
                onClick={() => setAmount(String(val))}
              >
                +${val}
              </button>
            ))}
          </div>

          <button type="submit" className="modal-submit-btn" disabled={isSubmitting}>
            {isSubmitting ? "Поповнюємо..." : "Поповнити баланс"}
          </button>
        </form>
      </div>
    </div>
  );
}
