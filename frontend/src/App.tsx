import { LoadingOverlay, ScrollArea } from "@mantine/core";
import { observer } from "mobx-react";
import { useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import "./App.css";
import { PrivateRoute } from "./components/routing/PrivateRoute";
import PublicRoute from "./components/routing/PublicRoute";
import { useLoading } from "./context/LoadingContext";
import useAuth from "./hooks/useAuth";
import AdminPanel from "./pages/AdminPanel";
import Auth from "./pages/Auth";
import { ConfirmEmail } from "./pages/ConfirmEmail";
import Home from "./pages/Home";
import Tracker from "./pages/Tracker";
import globalStore from "./stores/GlobalStore";

const App = observer(() => {
    const auth = useAuth();
    const { loading } = useLoading();

    useEffect(() => {
        auth.handleUserLoggedInCheck();
    }, []);

    if (globalStore.checkingAuth) {
        return (
            <LoadingOverlay
                visible
                overlayProps={{
                    opacity: 0.5,
                }}
                loaderProps={{
                    color: "gray",
                }}
            />
        );
    }

    return (
        <>
            <LoadingOverlay
                visible={loading}
                overlayProps={{
                    opacity: 0.5,
                }}
                loaderProps={{
                    color: "gray",
                }}
            />
            <ScrollArea h={"100vh"} p="xl">
                <BrowserRouter>
                    <Routes>
                        <Route
                            path="auth"
                            element={<PublicRoute page={<Auth />} />}
                        />
                        <Route
                            path="home"
                            element={<PrivateRoute page={<Home />} />}
                        />
                        <Route
                            path="trackers/:trackerId"
                            element={<PrivateRoute page={<Tracker />} />}
                        />
                        <Route
                            path="confirm-email"
                            element={<PublicRoute page={<ConfirmEmail />} />}
                        />
                        <Route
                            path="admin-panel"
                            element={<PrivateRoute page={<AdminPanel />} />}
                        />
                        <Route
                            path="*"
                            element={
                                globalStore.currentUser ? (
                                    <Navigate to={"/home"} />
                                ) : (
                                    <Navigate to={"/auth"} />
                                )
                            }
                        />
                    </Routes>
                </BrowserRouter>
            </ScrollArea>
        </>
    );
});

export default App;
