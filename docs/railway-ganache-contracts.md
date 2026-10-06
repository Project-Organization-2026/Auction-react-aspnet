# Unified USD and ETH demo auctions on Railway Ganache

Each active lot has one current price and one winner. USD bids use the application's balance; ETH bids use MetaMask and the lot's smart contract. For comparison, the backend converts ETH to USD at one fixed demo rate. A higher bid in either currency outbids the previous one.

The application deploys one `Auction.sol` contract per active lot and stores its address in `Lot.ContractAddress`. A background worker checks existing lots at startup and new lots every 30 seconds. It also raises an existing contract's ETH minimum if the database price has advanced. The contract's minimum is the current USD price plus the lot's minimum step, converted to wei; an ETH bid must exceed it.

## Railway web service variables

Set these on the **Auction-react-aspnet** service, not on Postgres or Ganache:

```text
ETHEREUM_RPC_URL=https://ganache-production.up.railway.app
VITE_GANACHE_RPC_URL=https://ganache-production.up.railway.app
ETHEREUM_AUTO_DEPLOY=true
ETH_USD_RATE_FIXED=true
ETH_USD_RATE=3000
```

Redeploy the web service after adding them. `VITE_GANACHE_RPC_URL` is compiled into the React bundle during the Docker build. The rate of `3000` means 1 demo ETH is worth $3,000 in this auction; use the same fixed value for the entire demo. The fixed rate defaults to `3000` even if its two variables are omitted, so an existing Railway deployment can upgrade safely. `ETHEREUM_AUTO_DEPLOY` is disabled by default for local development and other networks.

`ETHEREUM_DEPLOYER_ADDRESS` optionally selects one of Ganache's unlocked accounts; by default the first account is used. No private key is needed in Railway.

The Docker build compiles `Auction.sol` and copies the artifact into the ASP.NET image. After deploying, look for `Deployed auction contract ... for lot ...` in the web service logs. Open a lot and check that its API response has `contractAddress`. MetaMask must point to the same Ganache RPC and chain. The ETH form displays the contract's current minimum.

When a USD bid replaces an ETH bid, the contract makes the earlier ETH available through `pendingReturns`; the bidder can use **Withdraw** on the lot page. When an ETH bid replaces a USD bid, the earlier USD amount is returned to that user's application balance. The bid history shows ETH bids and their USD equivalents.

This is a demo exchange rate and demo settlement. When ETH wins, the app does not credit the seller's USD balance; the contract's `seller` is the Ganache deployer account, so an eventual on-chain payout belongs to that demo account. A full seller-wallet payout and production exchange-rate handling would require a separate settlement design.

Contracts deployed by an older version of the app cannot be upgraded in place. If an old contract still holds ETH bids, the worker leaves it untouched to preserve the funds, and bidding on that lot requires manual resolution. Previously recorded ETH bids to an address with `eth_getCode = 0x` are historical records; they cannot be reconstructed on a reset chain.

Ganache state must persist across service restarts. If the Ganache chain resets, old transaction history and contract addresses cease to be valid even though PostgreSQL keeps their records. The worker can redeploy contracts for active lots, but it cannot restore the old chain history.
