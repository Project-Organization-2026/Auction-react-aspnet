import { createContext, useContext, useState, useEffect, useCallback } from "react";
import { ethers } from "ethers";
import auctionContractArtifact from "../contracts/Auction.json";

const Web3Context = createContext(null);
const ganacheRpcUrl = import.meta.env.VITE_GANACHE_RPC_URL || "http://127.0.0.1:7545";

export function Web3Provider({ children }) {
  const [account, setAccount] = useState(null);
  const [balance, setBalance] = useState(null);
  const [chainId, setChainId] = useState(null);
  const [isConnecting, setIsConnecting] = useState(false);
  const [error, setError] = useState(null);

  const updateAccountData = useCallback(async (selectedAddress) => {
    if (!selectedAddress || typeof window.ethereum === "undefined") {
      setAccount(null);
      setBalance(null);
      return;
    }

    try {
      const provider = new ethers.BrowserProvider(window.ethereum);
      const ethBalance = await provider.getBalance(selectedAddress);
      const network = await provider.getNetwork();

      setAccount(selectedAddress);
      setBalance(parseFloat(ethers.formatEther(ethBalance)).toFixed(4));
      setChainId(Number(network.chainId));
      setError(null);
    } catch (err) {
      console.warn("Failed to retrieve wallet balance:", err);
    }
  }, []);

  const connectWallet = async () => {
    if (typeof window.ethereum === "undefined") {
      setError("MetaMask or Web3 wallet is not detected. Please install MetaMask.");
      return null;
    }

    setIsConnecting(true);
    setError(null);

    try {
      const accounts = await window.ethereum.request({
        method: "eth_requestAccounts",
      });

      if (accounts && accounts.length > 0) {
        await updateAccountData(accounts[0]);
        return accounts[0];
      }
    } catch (err) {
      console.error("Wallet connection error:", err);
      if (err.code === 4001) {
        setError("Connection request rejected by user.");
      } else {
        setError(err.message || "Failed to connect wallet.");
      }
    } finally {
      setIsConnecting(false);
    }
    return null;
  };

  const disconnectWallet = () => {
    setAccount(null);
    setBalance(null);
    setChainId(null);
    setError(null);
  };

  // Listen for account and network changes from MetaMask
  useEffect(() => {
    if (typeof window.ethereum === "undefined") return;

    // Check if already authorized
    window.ethereum
      .request({ method: "eth_accounts" })
      .then((accounts) => {
        if (accounts && accounts.length > 0) {
          updateAccountData(accounts[0]);
        }
      })
      .catch((err) => console.warn("Could not check accounts:", err));

    const handleAccountsChanged = (accounts) => {
      if (!accounts || accounts.length === 0) {
        disconnectWallet();
      } else {
        updateAccountData(accounts[0]);
      }
    };

    const handleChainChanged = () => {
      // Reload on network switch as recommended by MetaMask
      window.location.reload();
    };

    window.ethereum.on("accountsChanged", handleAccountsChanged);
    window.ethereum.on("chainChanged", handleChainChanged);

    return () => {
      if (window.ethereum.removeListener) {
        window.ethereum.removeListener("accountsChanged", handleAccountsChanged);
        window.ethereum.removeListener("chainChanged", handleChainChanged);
      }
    };
  }, [updateAccountData]);

  // Place on-chain bid using the deployed Auction smart contract
  const placeOnChainBid = async (amountInEth, expectedLotId, contractAddress) => {
    if (!account) {
      const connected = await connectWallet();
      if (!connected) throw new Error("Wallet not connected.");
    }

    if (!contractAddress || !ethers.isAddress(contractAddress)) {
      throw new Error("This lot does not have an auction contract yet. Try again shortly.");
    }

    const provider = new ethers.BrowserProvider(window.ethereum);
    const code = await provider.getCode(contractAddress);
    if (code === "0x") {
      throw new Error("Auction contract is not deployed at the configured address on this network. Switch to the correct Ganache network or update the contract address.");
    }
    const signer = await provider.getSigner();
    const contract = new ethers.Contract(
      contractAddress,
      auctionContractArtifact.abi,
      signer
    );

    const onChainLotId = await contract.lotId();
    if (onChainLotId !== BigInt(expectedLotId)) {
      throw new Error(`This contract belongs to lot #${onChainLotId}, not lot #${expectedLotId}.`);
    }

    const value = ethers.parseEther(String(amountInEth));
    let minimum;
    try {
      minimum = await contract.minimumBid();
    } catch {
      throw new Error("This lot uses an older auction contract. ETH bidding is unavailable until it is updated.");
    }
    if (value <= minimum) {
      throw new Error(`ETH bid must be greater than ${ethers.formatEther(minimum)} ETH.`);
    }
    const tx = await contract.placeBid({ value });
    const receipt = await tx.wait();

    // Refresh balance after transaction
    if (account) {
      await updateAccountData(account);
    }

    return { txHash: tx.hash, receipt };
  };

  const getPendingReturn = useCallback(async (contractAddress, walletAddress) => {
    if (!contractAddress || !walletAddress || !ethers.isAddress(contractAddress)) return 0n;
    const provider = new ethers.BrowserProvider(window.ethereum);
    if (await provider.getCode(contractAddress) === "0x") return 0n;
    const contract = new ethers.Contract(contractAddress, auctionContractArtifact.abi, provider);
    return contract.pendingReturns(walletAddress);
  }, []);

  const getMinimumBid = useCallback(async (contractAddress) => {
    if (!contractAddress || !ethers.isAddress(contractAddress)) return null;
    const provider = new ethers.BrowserProvider(window.ethereum);
    if (await provider.getCode(contractAddress) === "0x") return null;
    const contract = new ethers.Contract(contractAddress, auctionContractArtifact.abi, provider);
    return contract.minimumBid();
  }, []);

  const withdrawOutbidBid = async (contractAddress) => {
    const provider = new ethers.BrowserProvider(window.ethereum);
    const contract = new ethers.Contract(
      contractAddress, auctionContractArtifact.abi, await provider.getSigner());
    const tx = await contract.withdraw();
    await tx.wait();
    if (account) await updateAccountData(account);
    return tx.hash;
  };

  const switchToGanache = async () => {
    if (typeof window.ethereum === "undefined") return;
    const chainIdHex = "0x539"; // 1337 in hex
    try {
      await window.ethereum.request({
        method: "wallet_switchEthereumChain",
        params: [{ chainId: chainIdHex }],
      });
    } catch (switchError) {
      if (switchError.code === 4902 || switchError?.data?.originalError?.code === 4902) {
        try {
          await window.ethereum.request({
            method: "wallet_addEthereumChain",
            params: [
              {
                chainId: chainIdHex,
                chainName: "Ganache",
                nativeCurrency: {
                  name: "Ethereum",
                  symbol: "ETH",
                  decimals: 18,
                },
                rpcUrls: [ganacheRpcUrl],
              },
            ],
          });
        } catch (addError) {
          console.error("Failed to add Ganache network to MetaMask:", addError);
        }
      } else {
        console.error("Failed to switch network:", switchError);
      }
    }
  };

  const value = {
    account,
    balance,
    chainId,
    isConnected: !!account,
    isConnecting,
    error,
    connectWallet,
    disconnectWallet,
    switchToGanache,
    placeOnChainBid,
    getPendingReturn,
    getMinimumBid,
    withdrawOutbidBid,
  };

  return <Web3Context.Provider value={value}>{children}</Web3Context.Provider>;
}

export function useWeb3() {
  const context = useContext(Web3Context);
  if (!context) {
    throw new Error("useWeb3 must be used within a Web3Provider");
  }
  return context;
}
