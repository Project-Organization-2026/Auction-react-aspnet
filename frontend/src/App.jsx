import "./App.css";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { Web3Provider } from "./context/Web3Context";
import Header from "./components/Header";
import Footer from "./components/Footer";
import MainPage from "./pages/MainPage";
import LotDetailsPage from "./pages/LotDetailsPage";
import ProfilePage from "./pages/ProfilePage";
import CreateLotPage from "./pages/CreateLotPage";
import NotFoundPage from "./pages/NotFoundPage";

function App() {
  return (
    <AuthProvider>
      <Web3Provider>
        <BrowserRouter>
          <Header />
          <Routes>
            <Route path="/" element={<MainPage />} />
            <Route path="/lots/:id" element={<LotDetailsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/create-lot" element={<CreateLotPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
          <Footer />
        </BrowserRouter>
      </Web3Provider>
    </AuthProvider>
  );
}

export default App;
