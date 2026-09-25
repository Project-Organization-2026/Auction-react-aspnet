const Header = () => {
  return (
    <header className="site-header">
      <a className="brand" href="#top">
        <span className="brand-accent">best</span>
        <span>auction</span>
      </a>
      <button className="menu-button">Menu</button>
      <div className="search-box">
        <span>⌕</span>
        <input aria-label="Search" placeholder="Search" />
      </div>
      <button className="header-filter">⌖ Current</button>
      <button className="header-filter">♧ 100 miles</button>
      <button className="search-submit" aria-label="Submit search">⌕</button>
      <div className="header-actions">
        <button aria-label="Account">♙</button>
        <button aria-label="Wishlist">♡</button>
      </div>
    </header>
  );
};

export default Header;
