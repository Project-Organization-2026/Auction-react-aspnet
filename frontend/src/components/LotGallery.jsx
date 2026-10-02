import { getMainImage } from "../utils/lot.js";

function LotGallery({ lot }) {
  const image = getMainImage(lot);
  return (
    <div className="lot-gallery">
      {image ? (
        <img src={image.url} alt={lot.title} />
      ) : (
        <div className="lot-gallery__no-image">No image</div>
      )}
    </div>
  );
}

export default LotGallery;
