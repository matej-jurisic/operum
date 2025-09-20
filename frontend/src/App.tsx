import { AppShell } from "@mantine/core";
import { observer } from "mobx-react";
import { useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import "./App.css";
import OperumLoader from "./components/OperumLoader";
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
        return <OperumLoader visible />;
    }

    return (
        <>
            <OperumLoader visible={loading} />
            <AppShell h={"100vh"} w={"100vw"}>
                <AppShell.Main h="100%" p={"md"}>
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
                                element={
                                    <PublicRoute page={<ConfirmEmail />} />
                                }
                            />
                            <Route
                                path="admin-panel"
                                element={
                                    <Navigate to="/admin-panel/users" replace />
                                }
                            />
                            <Route
                                path="admin-panel/*"
                                element={
                                    <PrivateRoute
                                        allowedRoles={["admin"]}
                                        page={<AdminPanel />}
                                    />
                                }
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
                </AppShell.Main>
            </AppShell>
        </>
    );
});

export default App;
