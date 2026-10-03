import { network } from "hardhat";

async function main() {
  const { ethers } = await network.create();

  // Target address from argument, env variable, or the user's active address
  const argAddress = process.argv.find((a) => a.startsWith("0x") && a.length === 42);
  const targetAddress =
    argAddress ||
    process.env.FUND_ADDRESS ||
    "0x528bcee9d505a3793df6239d930800970aeeca8e";
  const amountEth = process.env.FUND_AMOUNT || "10";

  const [sender] = await ethers.getSigners();
  console.log(`\n--- Funding Wallet with Test ETH ---`);
  console.log(`Source Account: ${sender.address}`);
  console.log(`Target Address: ${targetAddress}`);
  console.log(`Amount: ${amountEth} ETH`);

  const tx = await sender.sendTransaction({
    to: targetAddress,
    value: ethers.parseEther(amountEth),
  });

  await tx.wait();
  console.log(`Transaction confirmed! Tx Hash: ${tx.hash}`);

  const balance = await ethers.provider.getBalance(targetAddress);
  console.log(`New Balance: ${ethers.formatEther(balance)} ETH\n`);
}

main().catch((error) => {
  console.error("Funding error:", error);
  process.exitCode = 1;
});
