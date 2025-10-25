import { AppShell } from "@mantine/core";
import { observer } from "mobx-react";
import { useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import AdminPanel from "./features/admin/pages/AdminPanel";
import useAuth from "./features/auth/hooks/useAuth";
import Auth from "./features/auth/pages/Auth";
import Home from "./features/home/pages/Home";
import Trackers from "./features/trackers/components/Trackers";
import Tracker from "./features/trackers/pages/Tracker";
import { ConfirmEmail } from "./features/users/pages/ConfirmEmail";
import OperumLoader from "./shared/components/OperumLoader";
import GenericRoute from "./shared/components/routing/GenericRoute";
import PrivateRoute from "./shared/components/routing/PrivateRoute";
import PublicRoute from "./shared/components/routing/PublicRoute";
import { useLoading } from "./shared/context/LoadingContext";
import globalStore from "./shared/stores/GlobalStore";

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
                                element={<GenericRoute page={<Home />} />}
                            />
                            <Route
                                path="trackers"
                                element={<PrivateRoute page={<Trackers />} />}
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
