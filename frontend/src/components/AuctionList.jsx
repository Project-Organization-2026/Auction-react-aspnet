import AuctionCard from "./AuctionCard";

function AuctionList({ lots }) {
  if (lots.length === 0) {
    return <div className="catalog-message" role="status">No auctions match these filters.</div>;
  }

  return <div className="auction-grid">
    {lots.map((lot) => <AuctionCard key={lot.id} lot={lot} />)}
  </div>;
}

export default AuctionList;
