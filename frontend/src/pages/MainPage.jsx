import { useEffect, useState, useCallback } from "react";
import AuctionList from "../components/AuctionList";
import AuctionToolbar from "../components/AuctionToolbar";
import { categoriesApi, lotsApi } from "../api";

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
        console.error("Не вдалося завантажити категорії", err);
      });
    return () => {
      isMounted = false;
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
      console.error("Помилка завантаження лотів", err);
      setError("Не вдалося завантажити лоти. Перевірте з'єднання з сервером.");
    } finally {
      setIsLoading(false);
    }
  }, [page, search, categoryId, status]);

  useEffect(() => {
    fetchLots();
  }, [fetchLots]);

  // Reset to page 1 on filter changes
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
        <h1 id="auctions-title">Аукціони</h1>

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
              Спробувати знову
            </button>
          </div>
        )}

        {isLoading ? (
          <div className="catalog-loading">
            <div className="spinner" />
            <p>Завантаження лотів...</p>
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
                  ← Попередня
                </button>
                <span className="pagination-info">
                  Сторінка {page} з {totalPages}
                </span>
                <button
                  type="button"
                  className="pagination-btn"
                  disabled={page >= totalPages}
                  onClick={() => setPage((prev) => Math.min(prev + 1, totalPages))}
                >
                  Наступна →
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
