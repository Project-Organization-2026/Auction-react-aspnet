import "./App.css";

const imageSources = [
  "https://d8iqbmvu05s9c.cloudfront.net/ajprhqgqg1otf7d5sm7u3brf27gv",
  "https://cdn.pixabay.com/photo/2014/06/03/19/38/board-361516_1280.jpg",
];

const bids = [
  ["Rebecca Ackroyd (b. 1987)", "Gerrard Street East", "pink"],
  ["Still Life With Gold", "King Street West", "gold"],
  ["Floral Memory", "Queen Street", "purple"],
  ["Abstract Form No. 4", "Dundas Street", "blue"],
  ["Green Horizon", "Gerrard Street East", "green"],
  ["Stacked Shapes", "King Street West", "orange"],
  ["The Visitor", "Queen Street", "sepia"],
  ["Autumn Garden", "Dundas Street", "red"],
];

function ArtworkImage({ imageIndex, alt }) {
  return (
    <img
      className="art-image"
      src={imageSources[imageIndex]}
      alt={alt}
      loading="lazy"
    />
  );
}

function BidCard({ title, location, imageIndex }) {
  return (
    <article className="bid-card">
      <ArtworkImage imageIndex={imageIndex} alt={title} />
      <div className="bid-card__body">
        <div className="bid-card__time">24h : 30m : 30s</div>
        <h3>{title}</h3>
        <p>{location}</p>
        <p className="bid-card__seller">Auction House</p>
        <div className="bid-card__footer">
          <div className="avatar-stack" aria-label="Active bidders">
            {[47, 49, 12, 13, 44].map((avatar, index) => (
              <span key={avatar}>
                <span className="avatar-fallback">
                  {["JD", "AK", "MK", "JS", "AL"][index]}
                </span>
                <img
                  src={`https://i.pravatar.cc/64?img=${avatar}`}
                  alt=""
                  loading="lazy"
                  onError={(event) => {
                    event.currentTarget.hidden = true;
                  }}
                />
              </span>
            ))}
          </div>
          <span className="bid-price">100+</span>
          <span className="bid-card__arrow" aria-hidden="true">
            ↗
          </span>
        </div>
      </div>
    </article>
  );
}

function HouseCard({ title, imageIndex, layout }) {
  return (
    <article className={`hero-house hero-house--${layout}`}>
      <div className="hero-house__media">
        <ArtworkImage imageIndex={imageIndex} alt={title} />
      </div>
      <div className="hero-house__details">
        <span>Auction On</span>
        <h2>{title}</h2>
        <p>Gerrard Street East</p>
        <a className="text-button" href="#bids">
          Explore Now <span>→</span>
        </a>
      </div>
    </article>
  );
}

function App() {
  return (
    <main className="app-shell">
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
      <section className="houses-section" aria-labelledby="houses-title">
        <div className="notice-bar">
          <span>←</span> Liquidate your estate or business within{" "}
          <strong>7 Days</strong> <span>→</span>
        </div>
        <h1 id="houses-title">Explore Houses</h1>
        <HouseCard
          title="Elegant Vintage Product"
          imageIndex={0}
          layout="image-top"
        />
        <HouseCard
          title="Vintage Product"
          imageIndex={1}
          layout="image-bottom"
        />
        <div className="houses-section__stack">
          <HouseCard
            title="Modern Ceramic"
            imageIndex={1}
            layout="image-left"
          />
          <HouseCard
            title="Botanical Study"
            imageIndex={0}
            layout="image-right"
          />
        </div>
      </section>
      <section className="bids-section" id="bids">
        <div className="section-heading">
          <h1>Bids Near You</h1>
          <a href="#all">
            View all <span>→</span>
          </a>
        </div>
        <div className="bid-grid">
          {bids.map(([title, location], index) => (
            <BidCard
              key={title}
              title={title}
              location={location}
              imageIndex={index % imageSources.length}
            />
          ))}
        </div>
      </section>
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
    </main>
  );
}

export default App;
