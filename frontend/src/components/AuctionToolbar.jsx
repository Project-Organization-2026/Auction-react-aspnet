import Icon from "./Icon";
import { LOT_STATUS } from "../utils/lot.js";

const ACTIVE = String(LOT_STATUS.ACTIVE);
const COMPLETED = String(LOT_STATUS.COMPLETED);

function AuctionToolbar({ search, onSearchChange, categoryId, onCategoryChange, categories, status, onStatusChange, count }) {
  return <div className="auction-controls">
    <div className="auction-controls__top">
      <div className="auction-tabs" aria-label="Auction status">
        <button type="button" className={status === "all" ? "active" : ""} aria-pressed={status === "all"} onClick={() => onStatusChange("all")}>All Auctions</button>
        <button type="button" className={status === ACTIVE ? "active" : ""} aria-pressed={status === ACTIVE} onClick={() => onStatusChange(ACTIVE)}>Active</button>
        <button type="button" className={status === COMPLETED ? "active" : ""} aria-pressed={status === COMPLETED} onClick={() => onStatusChange(COMPLETED)}>Completed</button>
      </div>
      <label className="catalog-search"><Icon name="search" size={18} /><input value={search} onChange={(event) => onSearchChange(event.target.value)} type="search" placeholder="Search auctions" aria-label="Search auctions" /></label>
    </div>
    <div className="auction-controls__bottom">
      <span role="status" aria-live="polite" aria-atomic="true">{count} {count === 1 ? "lot" : "lots"}</span>
      {categories.length > 0 && <label className="category-control">Category <select value={categoryId} onChange={(event) => onCategoryChange(event.target.value)} aria-label="Filter by category">
        <option value="">All categories</option>
        {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
      </select></label>}
    </div>
  </div>;
}

export default AuctionToolbar;
