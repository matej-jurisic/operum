import { AppShell } from "@mantine/core";
import { observer } from "mobx-react";
import { lazy, Suspense, useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import useAuth from "./features/auth/hooks/useAuth";
import OperumLoader from "./shared/components/OperumLoader";
import GenericRoute from "./shared/components/routing/GenericRoute";
import PrivateRoute from "./shared/components/routing/PrivateRoute";
import PublicRoute from "./shared/components/routing/PublicRoute";
import { useLoading } from "./shared/context/LoadingContext";
import globalStore from "./shared/stores/GlobalStore";

const AdminPanel = lazy(() => import("./features/admin/pages/AdminPanel"));
const Home = lazy(() => import("./features/home/pages/Home"));
const Trackers = lazy(() => import("./features/trackers/components/Trackers"));
const Tracker = lazy(() => import("./features/trackers/pages/Tracker"));
const ConfirmEmail = lazy(() =>
    import("./features/users/pages/ConfirmEmail").then((m) => ({
        default: m.ConfirmEmail,
    }))
);

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
                        <Suspense fallback={<OperumLoader visible />}>
                        <Routes>
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
                                path="trackers/:trackerId/*"
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
                                    <Navigate to="/admin-panel/overview" replace />
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
                                        <Navigate to={"/trackers"} />
                                    ) : (
                                        <Navigate to={"/home"} />
                                    )
                                }
                            />
                        </Routes>
                        </Suspense>
                    </BrowserRouter>
                </AppShell.Main>
            </AppShell>
        </>
    );
});

export default App;
