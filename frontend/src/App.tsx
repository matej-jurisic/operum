import { AppShell, useMantineColorScheme } from "@mantine/core";
import { observer } from "mobx-react";
import { lazy, Suspense, useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import useAuth from "./features/auth/hooks/useAuth";
import { areIntegrationsEnabled } from "./features/integrations/config/integrationsFeature";
import { readDefaultPage } from "./shared/constants/defaultPage";
import AppLayout from "./shared/components/navigation/AppLayout";
import OperumLoader from "./shared/components/OperumLoader";
import GenericRoute from "./shared/components/routing/GenericRoute";
import PrivateRoute from "./shared/components/routing/PrivateRoute";
import PublicRoute from "./shared/components/routing/PublicRoute";
import { useLoading } from "./shared/context/LoadingContext";
import globalStore from "./shared/stores/GlobalStore";

/** Bare shell for public pages (marketing home, legal, email confirmation). */
function PublicShell({ children }: { children: React.ReactNode }) {
    const location = useLocation();
    const { colorScheme } = useMantineColorScheme();
    const isHome = location.pathname === "/home";

    const dotPattern = colorScheme === "dark"
        ? "radial-gradient(circle, rgba(255,255,255,0.07) 1px, transparent 1px)"
        : "radial-gradient(circle, rgba(0,0,0,0.08) 1px, transparent 1px)";

    return (
        // 100% rather than 100vw: 100vw counts the vertical scrollbar, so any page tall
        // enough to scroll would also overflow sideways by the scrollbar's width.
        <AppShell h={"100vh"} w={"100%"} transitionDuration={0}>
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
const DashboardPage = lazy(() => import("./features/dashboard/pages/DashboardPage"));
const Home = lazy(() => import("./features/home/pages/Home"));
const IntegrationsPage = lazy(() => import("./features/integrations/pages/IntegrationsPage"));
const PrivacyPolicy = lazy(() => import("./features/legal/pages/PrivacyPolicy"));
const TermsOfService = lazy(() => import("./features/legal/pages/TermsOfService"));
const ProfilePage = lazy(() => import("./features/profile/pages/ProfilePage"));
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
                <Suspense fallback={<OperumLoader visible />}>
                    <Routes>
                        {/* Public pages -- no app chrome */}
                        <Route
                            path="home"
                            element={
                                <PublicShell>
                                    <GenericRoute page={<Home />} />
                                </PublicShell>
                            }
                        />
                        <Route
                            path="privacy"
                            element={
                                <PublicShell>
                                    <GenericRoute page={<PrivacyPolicy />} />
                                </PublicShell>
                            }
                        />
                        <Route
                            path="terms"
                            element={
                                <PublicShell>
                                    <GenericRoute page={<TermsOfService />} />
                                </PublicShell>
                            }
                        />
                        <Route
                            path="confirm-email"
                            element={
                                <PublicShell>
                                    <PublicRoute page={<ConfirmEmail />} />
                                </PublicShell>
                            }
                        />

                        {/* Signed-in app -- sidebar + command palette */}
                        <Route element={<AppLayout />}>
                            <Route
                                path="profile"
                                element={<PrivateRoute page={<ProfilePage />} />}
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
                                path="dashboard"
                                element={<PrivateRoute page={<DashboardPage />} />}
                            />
                            <Route
                                path="dashboard/:dashboardId"
                                element={<PrivateRoute page={<DashboardPage />} />}
                            />
                            {/* Gated at build time, so the route simply does not exist
                                when the feature is off -- the backend 404s it either way. */}
                            {areIntegrationsEnabled && (
                                <Route
                                    path="integrations"
                                    element={
                                        <PrivateRoute page={<IntegrationsPage />} />
                                    }
                                />
                            )}
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
                        </Route>

                        <Route
                            path="*"
                            element={
                                globalStore.currentUser ? (
                                    <Navigate to={readDefaultPage()} />
                                ) : (
                                    <Navigate to={"/home"} />
                                )
                            }
                        />
                    </Routes>
                </Suspense>
            </BrowserRouter>
        </>
    );
});

export default App;
