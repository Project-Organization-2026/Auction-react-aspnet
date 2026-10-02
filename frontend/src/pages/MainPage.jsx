import { useEffect, useState, useCallback } from "react";
import AuctionList from "../components/AuctionList";
import AuctionToolbar from "../components/AuctionToolbar";
import { categoriesApi, lotsApi } from "../api";
import { getAuctionHubConnection, startAuctionHub } from "../services/auctionHub";

function MainPage() {
  const [lots, setLots] = useState([]);
  const [categories, setCategories] = useState([]);
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [status, setStatus] = useState("all");
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  // Load categories once
  useEffect(() => {
    let isMounted = true;
    categoriesApi
      .getAll()
      .then((data) => {
        if (isMounted) setCategories(data);
      })
      .catch((err) => {
        console.error("Failed to load categories", err);
      });
    return () => {
      isMounted = false;
    };
  }, []);

  // SignalR real-time updates for catalog lots
  useEffect(() => {
    let isMounted = true;
    startAuctionHub();
    const hub = getAuctionHubConnection();

    const handleLotUpdated = (update) => {
      if (!isMounted) return;
      setLots((prev) =>
        prev.map((lot) =>
          lot.id === update.lotId
            ? { ...lot, currentPrice: update.currentPrice, winnerId: update.winnerId }
            : lot
        )
      );
    };

    hub.on("LotUpdated", handleLotUpdated);

    return () => {
      isMounted = false;
      hub.off("LotUpdated", handleLotUpdated);
    };
  }, []);

  // Fetch lots from backend API
  const fetchLots = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await lotsApi.getAll({
        page,
        pageSize: 12,
        search,
        categoryId: categoryId ? Number(categoryId) : undefined,
        status: status === "all" ? undefined : Number(status),
      });

      setLots(data.items || []);
      setTotalPages(data.totalPages || 1);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      console.error("Failed to load lots", err);
      setError("Failed to load auctions. Check your server connection.");
    } finally {
      setIsLoading(false);
    }
  }, [page, search, categoryId, status]);

  useEffect(() => {
    fetchLots();
  }, [fetchLots]);

  const handleSearchChange = (val) => {
    setSearch(val);
    setPage(1);
  };

  const handleCategoryChange = (val) => {
    setCategoryId(val);
    setPage(1);
  };

  const handleStatusChange = (val) => {
    setStatus(val);
    setPage(1);
  };

  return (
    <main className="page-main" id="top">
      <section className="auction-section" id="auctions" aria-labelledby="auctions-title">
        <h1 id="auctions-title">Auctions</h1>

        <AuctionToolbar
          search={search}
          onSearchChange={handleSearchChange}
          categoryId={categoryId}
          onCategoryChange={handleCategoryChange}
          categories={categories}
          status={status}
          onStatusChange={handleStatusChange}
          count={totalCount}
        />

        {error && (
          <div className="catalog-error" role="alert">
            <p>{error}</p>
            <button type="button" onClick={fetchLots} className="retry-btn">
              Try Again
            </button>
          </div>
        )}

        {isLoading ? (
          <div className="catalog-loading">
            <div className="spinner" />
            <p>Loading auctions...</p>
          </div>
        ) : (
          <>
            <AuctionList lots={lots} />

            {totalPages > 1 && (
              <div className="pagination">
                <button
                  type="button"
                  className="pagination-btn"
                  disabled={page <= 1}
                  onClick={() => setPage((prev) => Math.max(prev - 1, 1))}
                >
                  ← Previous
                </button>
                <span className="pagination-info">
                  Page {page} of {totalPages}
                </span>
                <button
                  type="button"
                  className="pagination-btn"
                  disabled={page >= totalPages}
                  onClick={() => setPage((prev) => Math.min(prev + 1, totalPages))}
                >
                  Next →
                </button>
              </div>
            )}
          </>
        )}
      </section>
    </main>
  );
}

export default MainPage;
