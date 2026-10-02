# Blockchain integration roadmap

Цей документ — тільки карта «куди що стане», без реалізації.
Іш'юси: #42 (Hardhat), #43 (`Auction.sol`), #44 (deploy + ABI export).

## Де що житиме

- `blockchain/` — Hardhat-проєкт: `contracts/Auction.sol`, `scripts/deploy.js`,
  `hardhat.config.js` (Ganache, порт 7545).
- `frontend/src/contracts/` — сюди deploy-скрипт писатиме адресу контракту
  та ABI; React імпортуватиме їх напряму. JSON-файли комітяться.
- Конфіг бекенда — секція `Blockchain` (`Enabled`, `ChainId`, `RpcUrl`,
  `ContractAddress`, `ConfirmationsRequired`) повернеться в `appsettings.json`
  та `.env.example`, коли з'явиться перший контракт.

## Шви інтеграції (що зачеплять іш'юси)

- Зв'язок лота з чейном — `Lot.Id` (int) як зовнішній ключ: контракт
  зберігатиме `lotId`, бекенд за потреби додасть міграцію з колонкою
  `OnChainAuctionId`. Окремої сутності не треба.
- Вхід гаманцем — окремий флоу поруч з парольним: `GET nonce` + `POST verify`
  (підпис → JWT). Ідентифікатор юзера — адреса гаманця.
- Ставки ончейн — `placeBid()`/`withdraw()`/`endAuction()` йдуть повз
  `BidsService`-баланс (патерн Pull over Push); бекенд лише дзеркалить події
  контракту для каталогу/історії.
