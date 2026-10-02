import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import LotGallery from "../components/LotGallery";
import { formatPrice } from "../utils/format.js";
import { LOT_STATUS, getEndLabel } from "../utils/lot.js";

function LotDetailsPage({ lots, onPlaceBid }) {
  const { id } = useParams();
  const lot = lots.find((item) => item.id === Number(id)) ?? null;
  // Form state is tied to the lot id, so opening another lot starts fresh.
  const [form, setForm] = useState({ id: null, amount: "", error: "" });
  const { amount, error } = form.id === id ? form : { amount: "", error: "" };

  if (lot === null) {
    return (
      <main className="not-found">
        <span className="not-found__code">404</span>
        <h1>Lot not found</h1>
        <p>There is no lot with this ID. Check the URL or return to the auction catalog.</p>
        <Link to="/">Back to auctions</Link>
      </main>
    );
  }

  const minBid = Number(lot.currentPrice ?? 0) + Number(lot.minBidStep ?? 0);

  const submit = (event) => {
    event.preventDefault();
    const value = Number(amount);
    if (!Number.isFinite(value) || value < minBid) {
      setForm({ id, amount, error: `Minimum bid is ${formatPrice(minBid)}` });
      return;
    }
    onPlaceBid(lot.id, value);
    setForm({ id, amount: "", error: "" });
  };

  return (
    <main className="page-main">
      <Link className="lot-back" to="/">
        ← Back to auctions
      </Link>
      <section className="lot-details">
        <LotGallery lot={lot} />
        <div className="bid-panel">
          {lot.category && <span className="bid-panel__category">{lot.category.name}</span>}
          <h1>{lot.title}</h1>
          <div className="bid-panel__price-label">Current price</div>
          <div className="bid-panel__price">{formatPrice(lot.currentPrice)}</div>
          <div className="bid-panel__end">{getEndLabel(lot)}</div>
          {lot.status === LOT_STATUS.ACTIVE ? (
            <form onSubmit={submit}>
              <label htmlFor="bid-amount">Your bid (min {formatPrice(minBid)})</label>
              <input
                id="bid-amount"
                type="number"
                min={minBid}
                step={lot.minBidStep || "any"}
                value={amount}
                onChange={(event) => setForm({ id, amount: event.target.value, error: "" })}
              />
              <button type="submit">Place bid</button>
              {error && <p role="alert">{error}</p>}
            </form>
          ) : (
            <p role="status">Bidding is closed for this lot.</p>
          )}
        </div>
      </section>
      {lot.description && <p className="lot-description">{lot.description}</p>}
    </main>
  );
}

export default LotDetailsPage;
