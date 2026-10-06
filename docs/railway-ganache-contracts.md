# ETH demo auctions on Railway Ganache

The application deploys one `Auction.sol` contract per active lot. The address is stored in `Lot.ContractAddress` and returned by the lot API. The browser uses that lot's address when placing an ETH bid. The background worker checks existing active lots at startup and repeats every 30 seconds, so newly activated lots are covered too.

## Railway web service variables

Set these on the **Auction-react-aspnet** service, not on Postgres or Ganache:

```text
ETHEREUM_RPC_URL=https://ganache-production.up.railway.app
VITE_GANACHE_RPC_URL=https://ganache-production.up.railway.app
ETHEREUM_AUTO_DEPLOY=true
```

`VITE_GANACHE_RPC_URL` is included in the React bundle during the Docker build. Rebuild the web service after changing it. `ETHEREUM_AUTO_DEPLOY` is deliberately disabled by default for local development and networks other than the demo Ganache node.

Optional: `ETHEREUM_STARTING_BID_ETH=0.01` sets the starting ETH bid for each new contract. The actual bid must be **greater** than this value. `ETHEREUM_DEPLOYER_ADDRESS` can select one of the unlocked addresses returned by Ganache's `eth_accounts`; otherwise the first account is used. Do not add a private key to Railway for this demo.

The Docker build compiles `Auction.sol` and copies the artifact into the ASP.NET image. After deploying, check the web service logs for `Deployed auction contract ... for lot ...`, then open an active lot and confirm its API response contains `contractAddress`. The ETH bid button stays unavailable until that address appears.

USD and ETH are separate demo auctions for each lot. ETH bids do not change the USD price or USD winner. The contract's `seller` is the Ganache deployer account, so ETH proceeds are held by that demo account when the contract is finalized; this is not a seller-wallet payout system. Previously recorded ETH bids to an address with `eth_getCode = 0x` are not repaired automatically.

Ganache state must persist across service restarts. If the Ganache chain resets, old transaction history and contract addresses cease to be valid even though PostgreSQL keeps their records. The worker can redeploy contracts for active lots, but it cannot restore the old chain history.
