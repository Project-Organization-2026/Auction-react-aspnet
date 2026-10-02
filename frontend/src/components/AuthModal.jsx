import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage } from "../utils/errors";

export default function AuthModal({ isOpen, onClose, initialMode = "login" }) {
  const [mode, setMode] = useState(initialMode);
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login, register } = useAuth();

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (mode === "register" && password.length < 8) {
      setError("Пароль має містити щонайменше 8 символів.");
      return;
    }

    setIsSubmitting(true);

    try {
      if (mode === "login") {
        await login(email, password);
      } else {
        if (!userName.trim()) {
          setError("Вкажіть ім'я користувача");
          setIsSubmitting(false);
          return;
        }
        await register(userName.trim(), email.trim(), password);
      }
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Помилка авторизації. Перевірте введені дані."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-tabs">
            <button
              type="button"
              className={`modal-tab ${mode === "login" ? "active" : ""}`}
              onClick={() => {
                setMode("login");
                setError("");
              }}
            >
              Вхід
            </button>
            <button
              type="button"
              className={`modal-tab ${mode === "register" ? "active" : ""}`}
              onClick={() => {
                setMode("register");
                setError("");
              }}
            >
              Реєстрація
            </button>
          </div>
          <button className="modal-close" onClick={onClose} aria-label="Закрити">
            ✕
          </button>
        </div>

        <form className="modal-form" onSubmit={handleSubmit}>
          {error && (
            <div className="modal-error" role="alert" style={{ whiteSpace: "pre-line" }}>
              {error}
            </div>
          )}

          {mode === "register" && (
            <div className="form-group">
              <label htmlFor="auth-username">Ім'я користувача</label>
              <input
                id="auth-username"
                type="text"
                placeholder="User123"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                required
              />
            </div>
          )}

          <div className="form-group">
            <label htmlFor="auth-email">Електронна пошта</label>
            <input
              id="auth-email"
              type="email"
              placeholder="user@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <label htmlFor="auth-password">Пароль</label>
              {mode === "register" && (
                <span className="field-hint">мін. 8 символів</span>
              )}
            </div>
            <input
              id="auth-password"
              type="password"
              placeholder="••••••••"
              minLength={mode === "register" ? 8 : 1}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          <button type="submit" className="modal-submit-btn" disabled={isSubmitting}>
            {isSubmitting
              ? "Зачекайте..."
              : mode === "login"
              ? "Увійти"
              : "Зареєструватися"}
          </button>
        </form>
      </div>
    </div>
  );
}
