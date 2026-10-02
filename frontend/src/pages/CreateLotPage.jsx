import { useState, useEffect } from "react";
import { useNavigate, Navigate, Link } from "react-router-dom";
import { categoriesApi, lotsApi } from "../api";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage } from "../utils/errors";

export default function CreateLotPage() {
  const { isAuthenticated, isLoading: authLoading, refreshUser } = useAuth();
  const navigate = useNavigate();

  const [categories, setCategories] = useState([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [startingPrice, setStartingPrice] = useState("10.00");
  const [minBidStep, setMinBidStep] = useState("1.00");
  const [durationDays, setDurationDays] = useState("7");
  const [imageUrl, setImageUrl] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    categoriesApi
      .getAll()
      .then((data) => {
        setCategories(data || []);
        if (data && data.length > 0) {
          setCategoryId(String(data[0].id));
        }
      })
      .catch((err) => console.error("Помилка завантаження категорій", err));
  }, []);

  if (authLoading) return null;
  if (!isAuthenticated) return <Navigate to="/" replace />;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    const price = Number(startingPrice);
    const step = Number(minBidStep);
    const days = Number(durationDays);

    if (!title.trim()) {
      setError("Вкажіть назву лота");
      return;
    }

    if (price <= 0 || step <= 0) {
      setError("Початкова ціна та крок ставки мають бути більше нуля");
      return;
    }

    // Calculate EndTime
    const endTime = new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString();

    setIsSubmitting(true);
    try {
      const createdLot = await lotsApi.create({
        title: title.trim(),
        description: description.trim() || undefined,
        startingPrice: price,
        minBidStep: step,
        endTime,
        categoryId: categoryId ? Number(categoryId) : undefined,
        status: 1, // Active
      });

      // If image URL is provided, add it to lot
      if (imageUrl && imageUrl.trim()) {
        try {
          await lotsApi.addImage(createdLot.id, {
            url: imageUrl.trim(),
            isMain: true,
          });
        } catch (imgErr) {
          console.warn("Не вдалося додати фото, але лот створено", imgErr);
        }
      }

      await refreshUser();
      navigate(`/lots/${createdLot.id}`);
    } catch (err) {
      setError(getErrorMessage(err, "Помилка створення лота. Перевірте введені дані."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="page-main">
      <Link className="lot-back" to="/profile">
        ← До особистого кабінету
      </Link>

      <section className="create-lot-section">
        <h1>Створення нового аукціону</h1>
        <p className="create-lot-sub">
          Заповніть інформацію про товар, щоб виставити його на торги
        </p>

        {error && <div className="bid-panel__alert error" role="alert">{error}</div>}

        <form className="create-lot-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="lot-title">Назва товару *</label>
            <input
              id="lot-title"
              type="text"
              placeholder="Наприклад: Vintage Rolex Submariner"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={512}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="lot-category">Категорія</label>
            <select
              id="lot-category"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
            >
              <option value="">Без категорії</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="lot-starting-price">Початкова ціна ($) *</label>
              <input
                id="lot-starting-price"
                type="number"
                min="0.01"
                step="0.01"
                value={startingPrice}
                onChange={(e) => setStartingPrice(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="lot-min-bid-step">Мінімальний крок ставки ($) *</label>
              <input
                id="lot-min-bid-step"
                type="number"
                min="0.01"
                step="0.01"
                value={minBidStep}
                onChange={(e) => setMinBidStep(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="lot-duration">Тривалість аукціону</label>
            <select
              id="lot-duration"
              value={durationDays}
              onChange={(e) => setDurationDays(e.target.value)}
            >
              <option value="1">1 день (24 години)</option>
              <option value="3">3 дні</option>
              <option value="7">7 днів (1 тиждень)</option>
              <option value="14">14 днів (2 тижні)</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="lot-image-url">Посилання на головне фото (URL)</label>
            <input
              id="lot-image-url"
              type="url"
              placeholder="https://images.unsplash.com/photo-..."
              value={imageUrl}
              onChange={(e) => setImageUrl(e.target.value)}
            />
            <span className="field-hint">Вкажіть пряме посилання на зображення (HTTP або HTTPS)</span>
          </div>

          <div className="form-group">
            <label htmlFor="lot-description">Опис лота</label>
            <textarea
              id="lot-description"
              rows={5}
              placeholder="Детальний опис товару, стан, комплектація, історія тощо..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={5000}
            />
          </div>

          <button type="submit" className="create-lot-submit-btn" disabled={isSubmitting}>
            {isSubmitting ? "Створення лота..." : "Опублікувати лот"}
          </button>
        </form>
      </section>
    </main>
  );
}
