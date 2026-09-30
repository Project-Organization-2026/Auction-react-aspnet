const price = (value) => new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value ?? 0);

function AuctionCard({ lot }) {
  const image = lot.images?.find((item) => item.isMain) ?? lot.images?.[0];
  const endTime = new Date(lot.endTime);
  const dateLabel = Number.isNaN(endTime.getTime()) ? "End date unavailable" : endTime.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
  return <article className="auction-card">
    <div className="auction-card__image-wrap">
      {image ? <img src={image.url} alt={lot.title} loading="lazy" /> : <div className="auction-card__no-image">No image</div>}
    </div>
    <div className="auction-card__content">
      <h3>{lot.title}</h3>
      {lot.category && <span className="auction-card__category">{lot.category.name}</span>}
      <div className="auction-card__price-label">Current price</div>
      <div className="auction-card__price"><strong>{price(lot.currentPrice)}</strong></div>
      <div className="auction-card__end">{lot.status === 2 ? "Ended" : "Ends"} {dateLabel}</div>
    </div>
  </article>;
}

export default AuctionCard;
