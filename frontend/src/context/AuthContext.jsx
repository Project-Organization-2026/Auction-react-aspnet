import { createContext, useContext, useEffect, useState, useCallback } from "react";
import { authApi, usersApi } from "../api";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem("token"));
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  const fetchProfile = useCallback(async () => {
    const currentToken = localStorage.getItem("token");
    if (!currentToken) {
      setUser(null);
      setIsLoading(false);
      return null;
    }
    try {
      const profile = await usersApi.getMe();
      setUser(profile);
      return profile;
    } catch (err) {
      console.error("Failed to fetch user profile", err);
      localStorage.removeItem("token");
      setToken(null);
      setUser(null);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchProfile();
  }, [token, fetchProfile]);

  useEffect(() => {
    const handleLogoutEvent = () => {
      setToken(null);
      setUser(null);
    };
    window.addEventListener("auth:logout", handleLogoutEvent);
    return () => window.removeEventListener("auth:logout", handleLogoutEvent);
  }, []);

  const login = async (email, password) => {
    const data = await authApi.login(email, password);
    const jwt = data?.accessToken || data?.token;
    if (jwt) {
      localStorage.setItem("token", jwt);
      setToken(jwt);
      if (data.user) {
        setUser(data.user);
      }
      setIsLoading(true);
      const profile = await fetchProfile();
      return profile || data.user;
    }
    throw new Error("No access token received from server");
  };

  const register = async (userName, email, password) => {
    const data = await authApi.register(userName, email, password);
    const jwt = data?.accessToken || data?.token;
    if (jwt) {
      localStorage.setItem("token", jwt);
      setToken(jwt);
      if (data.user) {
        setUser(data.user);
      }
      setIsLoading(true);
      const profile = await fetchProfile();
      return profile || data.user;
    }
    throw new Error("No access token received from server");
  };

  const logout = () => {
    localStorage.removeItem("token");
    setToken(null);
    setUser(null);
  };

  const refreshUser = async () => {
    return await fetchProfile();
  };

  const topUpBalance = async (amount) => {
    const res = await usersApi.topUpBalance(amount);
    setUser((prev) => (prev ? { ...prev, balance: res.balance } : null));
    return res.balance;
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        user,
        isAuthenticated: !!token && !!user,
        isAdmin: user?.role === 1 || user?.role === "Admin",
        isLoading,
        login,
        register,
        logout,
        refreshUser,
        topUpBalance,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
