function LotGallery({ lot }) {
  const image = lot.images?.find((item) => item.isMain) ?? lot.images?.[0];
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
