import { AppShell, useMantineColorScheme } from "@mantine/core";
import { observer } from "mobx-react";
import { lazy, Suspense, useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import useAuth from "./features/auth/hooks/useAuth";
import OperumLoader from "./shared/components/OperumLoader";
import GenericRoute from "./shared/components/routing/GenericRoute";
import PrivateRoute from "./shared/components/routing/PrivateRoute";
import PublicRoute from "./shared/components/routing/PublicRoute";
import { useLoading } from "./shared/context/LoadingContext";
import globalStore from "./shared/stores/GlobalStore";

function AppShellLayout({ children }: { children: React.ReactNode }) {
    const location = useLocation();
    const { colorScheme } = useMantineColorScheme();
    const isHome = location.pathname === "/home";

    const dotPattern = colorScheme === "dark"
        ? "radial-gradient(circle, rgba(255,255,255,0.07) 1px, transparent 1px)"
        : "radial-gradient(circle, rgba(0,0,0,0.08) 1px, transparent 1px)";

    return (
        <AppShell h={"100vh"} w={"100vw"} transitionDuration={0}>
            <AppShell.Main
                h="100%"
                p={isHome ? 0 : "md"}
                style={{
                    backgroundImage: isHome ? undefined : dotPattern,
                    backgroundSize: "28px 28px",
                }}
            >
                {children}
            </AppShell.Main>
        </AppShell>
    );
}

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
            <BrowserRouter>
                <AppShellLayout>
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
                </AppShellLayout>
            </BrowserRouter>
        </>
    );
});

export default App;
