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
  const [imageMode, setImageMode] = useState("file"); // "file" | "url"
  const [imageFile, setImageFile] = useState(null);
  const [imagePreview, setImagePreview] = useState("");
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
      .catch((err) => console.error("Failed to load categories", err));
  }, []);

  const handleFileChange = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 10 * 1024 * 1024) {
      setError("Image file exceeds 10MB limit.");
      return;
    }
    setImageFile(file);
    setImagePreview(URL.createObjectURL(file));
    setError("");
  };

  const handleRemoveFile = () => {
    setImageFile(null);
    if (imagePreview) {
      URL.revokeObjectURL(imagePreview);
      setImagePreview("");
    }
  };

  if (authLoading) return null;
  if (!isAuthenticated) return <Navigate to="/" replace />;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    const price = Number(startingPrice);
    const step = Number(minBidStep);
    const days = Number(durationDays);

    if (!title.trim()) {
      setError("Please provide an item title.");
      return;
    }

    if (price <= 0 || step <= 0) {
      setError("Starting price and bid step must be greater than zero.");
      return;
    }

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

      if (imageMode === "file" && imageFile) {
        try {
          await lotsApi.uploadImage(createdLot.id, imageFile, true);
        } catch (uploadErr) {
          console.warn("Could not upload image, but lot was created", uploadErr);
        }
      } else if (imageMode === "url" && imageUrl && imageUrl.trim()) {
        try {
          await lotsApi.addImage(createdLot.id, {
            url: imageUrl.trim(),
            isMain: true,
          });
        } catch (imgErr) {
          console.warn("Could not attach image, but lot was created", imgErr);
        }
      }

      await refreshUser();
      navigate(`/lots/${createdLot.id}`);
    } catch (err) {
      setError(getErrorMessage(err, "Failed to create auction. Please check your inputs."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="page-main">
      <Link className="lot-back" to="/profile">
        ← Back to profile
      </Link>

      <section className="create-lot-section">
        <h1>Create New Auction</h1>
        <p className="create-lot-sub">
          Fill in the details below to list your item for live bidding
        </p>

        {error && <div className="bid-panel__alert error" role="alert">{error}</div>}

        <form className="create-lot-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="lot-title">Item Title *</label>
            <input
              id="lot-title"
              type="text"
              placeholder="e.g. Vintage 1968 Omega Speedmaster"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={512}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="lot-category">Category</label>
            <select
              id="lot-category"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
            >
              <option value="">No category</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="lot-starting-price">Starting Price ($) *</label>
              <input
                id="lot-starting-price"
                type="number"
                min="0.01"
                step="0.01"
                value={startingPrice}
                onChange={(e) => setStartingPrice(e.target.value)}
                onBlur={() => {
                  const val = parseFloat(startingPrice);
                  if (!isNaN(val) && val > 0) setStartingPrice(val.toFixed(2));
                }}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="lot-min-bid-step">Minimum Bid Step ($) *</label>
              <input
                id="lot-min-bid-step"
                type="number"
                min="0.01"
                step="0.01"
                value={minBidStep}
                onChange={(e) => setMinBidStep(e.target.value)}
                onBlur={() => {
                  const val = parseFloat(minBidStep);
                  if (!isNaN(val) && val > 0) setMinBidStep(val.toFixed(2));
                }}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="lot-duration">Auction Duration</label>
            <select
              id="lot-duration"
              value={durationDays}
              onChange={(e) => setDurationDays(e.target.value)}
            >
              <option value="1">1 day (24 hours)</option>
              <option value="3">3 days</option>
              <option value="7">7 days (1 week)</option>
              <option value="14">14 days (2 weeks)</option>
            </select>
          </div>

          <div className="form-group">
            <label>Item Image</label>
            <div className="image-mode-tabs">
              <button
                type="button"
                className={`image-tab-btn ${imageMode === "file" ? "active" : ""}`}
                onClick={() => setImageMode("file")}
              >
                📁 Upload File
              </button>
              <button
                type="button"
                className={`image-tab-btn ${imageMode === "url" ? "active" : ""}`}
                onClick={() => setImageMode("url")}
              >
                🔗 Image URL
              </button>
            </div>

            {imageMode === "file" ? (
              <div className="file-upload-box">
                {imagePreview ? (
                  <div className="file-upload-preview">
                    <img src={imagePreview} alt="Preview" />
                    <div className="file-upload-info">
                      <span className="file-name">{imageFile?.name}</span>
                      <span className="file-size">
                        {(imageFile?.size / 1024).toFixed(1)} KB
                      </span>
                      <button
                        type="button"
                        className="file-remove-btn"
                        onClick={handleRemoveFile}
                      >
                        Remove
                      </button>
                    </div>
                  </div>
                ) : (
                  <label className="file-upload-dropzone">
                    <input
                      type="file"
                      accept="image/png, image/jpeg, image/webp, image/gif"
                      onChange={handleFileChange}
                      style={{ display: "none" }}
                    />
                    <div className="dropzone-content">
                      <span className="dropzone-icon">🖼️</span>
                      <span className="dropzone-text">Click to choose image or drag & drop</span>
                      <span className="field-hint">PNG, JPG, WEBP, GIF up to 10MB</span>
                    </div>
                  </label>
                )}
              </div>
            ) : (
              <div>
                <input
                  id="lot-image-url"
                  type="url"
                  placeholder="https://images.unsplash.com/photo-..."
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                />
                <span className="field-hint">Direct link to an image (HTTP or HTTPS)</span>
              </div>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="lot-description">Description</label>
            <textarea
              id="lot-description"
              rows={5}
              placeholder="Provide condition, provenance, technical specifications..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={5000}
            />
          </div>

          <button type="submit" className="create-lot-submit-btn" disabled={isSubmitting}>
            {isSubmitting ? "Publishing..." : "Publish Auction"}
          </button>
        </form>
      </section>
    </main>
  );
}
