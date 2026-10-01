import { useEffect, useMemo, useState } from "react";
import "./App.css";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import Header from "./components/Header";
import Footer from "./components/Footer";
import MainPage from "./pages/MainPage";
import LotDetailsPage from "./pages/LotDetailsPage";
import NotFoundPage from "./pages/NotFoundPage";
import { testAuctions as initialLots } from "./data/testAuctions";

const loadPrices = () => {
  try {
    const parsed = JSON.parse(localStorage.getItem("bidPrices") ?? "{}");
    if (parsed === null || typeof parsed !== "object" || Array.isArray(parsed)) {
      return {};
    }
    return Object.fromEntries(
      Object.entries(parsed).filter(([, value]) => Number.isFinite(value)),
    );
  } catch {
    return {};
  }
};

function App() {
  // Single source of truth for bids: id -> currentPrice, shared by all pages.
  const [prices, setPrices] = useState(loadPrices);
  const lots = useMemo(
    () =>
      initialLots.map((lot) =>
        prices[lot.id] != null ? { ...lot, currentPrice: prices[lot.id] } : lot,
      ),
    [prices],
  );

  const placeBid = (id, value) => {
    setPrices((prev) => ({ ...prev, [id]: value }));
  };

  useEffect(() => {
    try {
      localStorage.setItem("bidPrices", JSON.stringify(prices));
    } catch {
      // Storage full or disabled: in-memory prices still work for this session.
    }
  }, [prices]);

  return (
    <BrowserRouter>
      <Header />
      <Routes>
        <Route path="/" element={<MainPage lots={lots} />} />
        <Route path="/lots/:id" element={<LotDetailsPage lots={lots} onPlaceBid={placeBid} />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      <Footer />
    </BrowserRouter>
  );
}

export default App;
