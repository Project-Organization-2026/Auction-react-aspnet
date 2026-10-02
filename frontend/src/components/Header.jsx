import { useLocation } from "react-router-dom";

function Header() {
  const { pathname } = useLocation();
  return <>
    <div className="announcement">Discover lots from our live auctions</div>
    <header className="site-header">
      <div className="site-header__inner">
        <a className="brand" href="/" aria-label="Bestauction home"><span className="brand-mark">+</span><span>bestauction</span></a>
        <nav className="main-nav" aria-label="Main navigation"><a href="/#auctions" aria-current={pathname === "/" ? "page" : undefined}>Auctions</a></nav>
        <a className="header-cta" href="/#auctions">Explore auctions</a>
      </div>
    </header>
  </>;
}

export default Header;
