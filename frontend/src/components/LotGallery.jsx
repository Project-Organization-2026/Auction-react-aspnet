import { useState, useEffect } from "react";
import { getMainImage } from "../utils/lot.js";

function LotGallery({ lot }) {
  const images = lot?.images || [];
  const mainImage = getMainImage(lot);
  const [selectedImage, setSelectedImage] = useState(mainImage?.url || null);

  useEffect(() => {
    setSelectedImage(mainImage?.url || null);
  }, [lot?.id, mainImage?.url]);

  return (
    <div className="lot-gallery">
      <div className="lot-gallery__main">
        {selectedImage ? (
          <img src={selectedImage} alt={lot?.title} />
        ) : (
          <div className="lot-gallery__no-image">Зображення відсутнє</div>
        )}
      </div>

      {images.length > 1 && (
        <div className="lot-gallery__thumbs">
          {images.map((img) => (
            <button
              type="button"
              key={img.id}
              className={`lot-gallery__thumb ${selectedImage === img.url ? "active" : ""}`}
              onClick={() => setSelectedImage(img.url)}
            >
              <img src={img.url} alt="мініатюра" />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export default LotGallery;
