import hardhatToolboxMochaEthersPlugin from "@nomicfoundation/hardhat-toolbox-mocha-ethers";
import { defineConfig } from "hardhat/config";

export default defineConfig({
    plugins: [hardhatToolboxMochaEthersPlugin],
    solidity: {
        profiles: {
            default: {
                version: "0.8.24",
                settings: {
                    evmVersion: "paris",
                },
            },
        },
    },
    networks: {
        ganache: {
            type: "http",
            url: "http://127.0.0.1:7545",
            chainId: 1337,
        },
    },
});