function NotFoundPage() {
  return <main className="not-found">
    <span className="not-found__code">404</span>
    <h1>Page not found</h1>
    <p>There is no page at this address. Check the URL or return to the auction catalog.</p>
    <a href="/">Back to auctions</a>
  </main>;
}

export default NotFoundPage;
