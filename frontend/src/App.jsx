import "./App.css";
import Header from "./components/Header";
import Footer from "./components/Footer";
import MainPage from "./pages/MainPage";

function App() {
  return (
    <main className="app-shell">
      <Header />
      <MainPage />
      <Footer />
    </main>
  );
}

export default App;
