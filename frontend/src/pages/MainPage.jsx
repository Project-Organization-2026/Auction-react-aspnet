import { useMemo, useState } from "react";
import AuctionList from "../components/AuctionList";
import AuctionToolbar from "../components/AuctionToolbar";

function MainPage({ lots }) {
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [status, setStatus] = useState("all");

  const categories = useMemo(
    () => Array.from(
      new Map(lots.filter((lot) => lot.category).map((lot) => [lot.category.id, lot.category])).values(),
    ),
    [lots],
  );

  const visibleAuctions = useMemo(() => {
    const term = search.trim().toLowerCase();
    return lots.filter((lot) => {
      const matchesSearch = !term || `${lot.title} ${lot.description}`.toLowerCase().includes(term);
      const matchesCategory = !categoryId || lot.category?.id === Number(categoryId);
      const matchesStatus = status === "all" || lot.status === Number(status);
      return matchesSearch && matchesCategory && matchesStatus;
    });
  }, [search, categoryId, status, lots]);

  return <main className="page-main" id="top">
    <section className="auction-section" id="auctions" aria-labelledby="auctions-title">
      <h1 id="auctions-title">Auctions</h1>
      <AuctionToolbar
        search={search}
        onSearchChange={setSearch}
        categoryId={categoryId}
        onCategoryChange={setCategoryId}
        categories={categories}
        status={status}
        onStatusChange={setStatus}
        count={visibleAuctions.length}
      />
      <AuctionList lots={visibleAuctions} />
    </section>
  </main>;
}

export default MainPage;
