import { useMemo, useState } from "react";
import AuctionList from "../components/AuctionList";
import AuctionToolbar from "../components/AuctionToolbar";
import { testAuctions } from "../data/testAuctions";

const categories = Array.from(
  new Map(testAuctions.filter((lot) => lot.category).map((lot) => [lot.category.id, lot.category])).values(),
);

function MainPage() {
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [status, setStatus] = useState("all");

  const visibleAuctions = useMemo(() => {
    const term = search.trim().toLowerCase();
    return testAuctions.filter((lot) => {
      const matchesSearch = !term || `${lot.title} ${lot.description}`.toLowerCase().includes(term);
      const matchesCategory = !categoryId || lot.category?.id === Number(categoryId);
      const matchesStatus = status === "all" || lot.status === Number(status);
      return matchesSearch && matchesCategory && matchesStatus;
    });
  }, [search, categoryId, status]);

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
