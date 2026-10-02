import { Link } from "react-router-dom";
import { formatPrice } from "../utils/format.js";
import { LOT_STATUS, getEndLabel, getMainImage } from "../utils/lot.js";

function AuctionCard({ lot }) {
  const image = getMainImage(lot);
  const detailsUrl = `/lots/${lot.id}`;
  return <article className="auction-card">
    <Link className="auction-card__image-wrap" to={detailsUrl} aria-label={lot.title}>
      {image ? <img src={image.url} alt={lot.title} loading="lazy" /> : <div className="auction-card__no-image">No image</div>}
    </Link>
    <div className="auction-card__content">
      <h3><Link to={detailsUrl}>{lot.title}</Link></h3>
      {lot.category && <span className="auction-card__category">{lot.category.name}</span>}
      <div className="auction-card__price-label">Current price</div>
      <div className="auction-card__price"><strong>{formatPrice(lot.currentPrice)}</strong></div>
      <div className="auction-card__end">{getEndLabel(lot)}</div>
      {lot.status === LOT_STATUS.ACTIVE && <Link className="auction-card__bid-button" to={detailsUrl}>Place Bid</Link>}
    </div>
  </article>;
}

export default AuctionCard;
