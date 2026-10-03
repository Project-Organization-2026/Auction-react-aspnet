import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { formatPrice } from "../utils/format";
import { getErrorMessage } from "../utils/errors";

export default function TopUpModal({ isOpen, onClose }) {
  const [amount, setAmount] = useState("100.00");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const { topUpBalance, user } = useAuth();

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    const val = Number(amount);
    if (!Number.isFinite(val) || val <= 0) {
      setError("Amount must be greater than 0");
      return;
    }

    setIsSubmitting(true);
    setError("");

    try {
      await topUpBalance(val);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Failed to top up balance."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddAmount = (addVal) => {
    const current = parseFloat(amount);
    const next = (Number.isFinite(current) && current > 0 ? current : 0) + addVal;
    setAmount(next.toFixed(2));
    setError("");
  };

  const handleBlur = () => {
    const num = parseFloat(amount);
    if (Number.isFinite(num) && num > 0) {
      setAmount(num.toFixed(2));
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Top Up Balance</h2>
          <button className="modal-close" onClick={onClose} aria-label="Close">
            ✕
          </button>
        </div>

        <p className="modal-sub">
          Current balance: <strong>{formatPrice(user?.balance ?? 0)}</strong>
        </p>

        <form className="modal-form" onSubmit={handleSubmit}>
          {error && <div className="modal-error" role="alert">{error}</div>}

          <div className="form-group">
            <label htmlFor="topup-amount">Deposit Amount ($)</label>
            <input
              id="topup-amount"
              type="number"
              min="0.01"
              step="0.01"
              value={amount}
              onChange={(e) => {
                setAmount(e.target.value);
                setError("");
              }}
              onBlur={handleBlur}
              placeholder="0.00"
              required
            />
          </div>

          <div className="quick-amounts">
            {[50, 100, 250, 500].map((val) => (
              <button
                type="button"
                key={val}
                className="quick-amount-btn"
                onClick={() => handleAddAmount(val)}
              >
                +${val}
              </button>
            ))}
          </div>

          <button type="submit" className="modal-submit-btn" disabled={isSubmitting}>
            {isSubmitting ? "Processing..." : "Deposit Funds"}
          </button>
        </form>
      </div>
    </div>
  );
}
