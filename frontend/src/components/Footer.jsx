const Footer = () => {
  return (
    <>
      <footer className="site-footer">
        <div>
          <a className="brand" href="#top">
            <span className="brand-accent">best</span>
            <span>auction</span>
          </a>
          <p>
            Bestauction is the new way to discover
            <br />
            and collect remarkable things.
          </p>
        </div>
        <div>
          <h4>Auctions</h4>
          <a href="#all">All Auctions</a>
          <a href="#ending">Ending Soon</a>
          <a href="#categories">Categories</a>
        </div>
        <div>
          <h4>Resources</h4>
          <a href="#about">About</a>
          <a href="#help">Help Center</a>
          <a href="#contact">Contact Us</a>
        </div>
        <div>
          <h4>Are you looking to</h4>
          <button className="outline-button">Sell</button>
        </div>
      </footer>
      <div className="footer-bottom">
        <span>© 2026 Bestauction. All rights reserved.</span>
        <span>Terms &nbsp; Privacy Policy &nbsp; Legal Disclaimer</span>
      </div>
    </>
  );
};

export default Footer;
