import { network } from "hardhat";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function main() {
  const { ethers } = await network.create();

  console.log("Розгортання Auction.sol в мережу...");

  const lotId = 1;
  const startingPrice = ethers.parseEther("0.1"); // 0.1 ETH
  const durationInSeconds = 7 * 24 * 60 * 60; // 7 днів

  const auctionFactory = await ethers.getContractFactory("Auction");
  const auction = await auctionFactory.deploy(lotId, startingPrice, durationInSeconds);
  await auction.waitForDeployment();

  const contractAddress = await auction.getAddress();
  console.log(`Контракт Auction успішно розгорнуто за адресою: ${contractAddress}`);

  // Отримання артефакту з ABI
  const artifactPath = path.resolve(
    __dirname,
    "../artifacts/contracts/Auction.sol/Auction.json"
  );

  let abi = [];
  if (fs.existsSync(artifactPath)) {
    const artifact = JSON.parse(fs.readFileSync(artifactPath, "utf-8"));
    abi = artifact.abi;
  }

  // Експорт у frontend/src/contracts/Auction.json
  const frontendContractsDir = path.resolve(__dirname, "../../frontend/src/contracts");
  if (!fs.existsSync(frontendContractsDir)) {
    fs.mkdirSync(frontendContractsDir, { recursive: true });
  }

  const exportData = {
    address: contractAddress,
    abi,
    lotId,
    network: network.name,
    deployedAt: new Date().toISOString(),
  };

  const targetFile = path.join(frontendContractsDir, "Auction.json");
  fs.writeFileSync(targetFile, JSON.stringify(exportData, null, 2), "utf-8");
  console.log(`ABI та адресу контракту збережено у: ${targetFile}`);
}

main().catch((error) => {
  console.error("Помилка під час розгортання:", error);
  process.exitCode = 1;
});
