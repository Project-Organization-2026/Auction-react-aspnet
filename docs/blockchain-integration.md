# MetaMask and smart-contract integration plan

The backend is prepared for a hybrid auction model, but blockchain execution is
disabled by default. Existing auctions use `OffChain` settlement and keep the
current PostgreSQL balance flow. A future smart-contract auction must use
`Blockchain` settlement and treat confirmed contract events as its source of
truth.

## Prepared backend contract

- A user can store one verified Ethereum wallet (`WalletAddress`,
  `WalletVerifiedAt`). There is intentionally no endpoint that writes these
  fields yet: accepting an address without a signature would let a user claim
  somebody else's wallet.
- A lot stores its settlement mode, chain ID, contract address, uint256 auction
  ID and creation/settlement transaction hashes.
- A bid can store an exact uint256 amount in wei, its transaction hash and block
  number. `AmountWei` is text because a Solidity `uint256` is larger than a C#
  `decimal`; never calculate on-chain values through `double` or `decimal`.
- Unique indexes prevent one wallet, on-chain auction or bid transaction from
  being imported twice.
- The current REST bidding and settlement methods reject blockchain lots. This
  prevents the database balance flow and the smart contract from charging or
  settling the same auction independently.

## Work to do after issues #42-#44

1. **Define the Solidity contract API.** At minimum it needs auction creation,
   payable bidding, finalization/cancellation, pull-based refunds/withdrawals,
   access control, pause protection and events such as `AuctionCreated`,
   `BidPlaced`, `AuctionFinalized` and `RefundWithdrawn`.
2. **Deploy and export artifacts.** Keep the contract ABI and deployed address
   as generated artifacts/configuration. Never commit a deployer private key or
   a funded Ganache/Hardhat mnemonic.
3. **Add wallet challenge verification.** Recommended endpoints:
   `POST /api/wallet/challenge` and `POST /api/wallet/verify`. The backend must
   issue a single-use, short-lived nonce bound to the authenticated user,
   reconstruct the exact message, recover the signer from the EIP-191 or SIWE
   signature, compare addresses, consume the nonce and only then save the
   normalized wallet address.
4. **Create blockchain auctions through a dedicated flow.** Do not extend the
   ordinary `POST /api/lots` request with arbitrary contract metadata. A
   blockchain service must verify the configured chain and contract, submit or
   observe `createAuction`, then persist the values from the confirmed event.
5. **Synchronize events idempotently.** Add a hosted worker or webhook/indexer
   that scans from the last processed block, waits for the configured number of
   confirmations and upserts events by transaction hash. It must handle retries
   and chain reorganizations.
6. **Use the contract for blockchain bids.** MetaMask sends `placeBid` directly.
   The API does not deduct `User.Balance`; it records a bid only after receipt
   and event validation (contract address, chain ID, auction ID, sender, amount,
   success status and confirmations).
7. **Finalize from contract events.** For blockchain lots, the backend mirrors
   the winner and status from the confirmed finalization event. It must not
   credit the seller's database balance.
8. **Add integration tests.** Run contract tests against Hardhat/Ganache and API
   tests for invalid signatures, replayed nonces, wrong networks, reverted
   transactions, duplicate events, delayed confirmations and reorg recovery.

## Environment variables

The API already accepts these variables, but keeps them inactive until a real
deployment exists:

```text
Blockchain__Enabled=false
Blockchain__ChainId=31337
Blockchain__RpcUrl=http://127.0.0.1:8545
Blockchain__ContractAddress=0x...
Blockchain__ConfirmationsRequired=2
```

When using Ganache instead of Hardhat, set the actual chain ID reported by the
node; do not assume that both local networks use the same value.
