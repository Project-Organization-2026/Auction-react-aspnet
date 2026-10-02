# Blockchain

Hardhat-проєкт живе в цій папці (issue #42):

- `contracts/` — Solidity-контракти (`Auction.sol`, issue #43);
- `scripts/deploy.js` — деплой у Ganache + експорт адреси й ABI
  у `frontend/src/contracts/` (issue #44);
- `hardhat.config.js` — підключення до локальної мережі Ganache (порт 7545).

Згенеровані `artifacts/` і `cache/` в гіт не комітяться (див. кореневий `.gitignore`).
