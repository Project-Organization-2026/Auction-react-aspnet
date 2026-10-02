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

    if (mode === "register") {
      if (!userName.trim()) {
        setError("Username is required.");
        return;
      }
      if (password.length < 8) {
        setError("Password must contain at least 8 characters.");
        return;
      }
    }

    setIsSubmitting(true);

    try {
      if (mode === "login") {
        await login(email.trim(), password);
      } else {
        await register(userName.trim(), email.trim(), password);
      }
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Authentication failed. Please check your credentials."));
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
              Sign In
            </button>
            <button
              type="button"
              className={`modal-tab ${mode === "register" ? "active" : ""}`}
              onClick={() => {
                setMode("register");
                setError("");
              }}
            >
              Register
            </button>
          </div>
          <button className="modal-close" onClick={onClose} aria-label="Close">
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
              <label htmlFor="auth-username">Username</label>
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
            <label htmlFor="auth-email">Email Address</label>
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
              <label htmlFor="auth-password">Password</label>
              {mode === "register" && (
                <span className="field-hint">min. 8 characters</span>
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
              ? "Please wait..."
              : mode === "login"
              ? "Sign In"
              : "Create Account"}
          </button>
        </form>
      </div>
    </div>
  );
}
