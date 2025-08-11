import { Container, LoadingOverlay, ScrollArea } from "@mantine/core";
import { observer } from "mobx-react";
import { useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import "./App.css";
import { PrivateRoute } from "./components/routing/PrivateRoute";
import PublicRoute from "./components/routing/PublicRoute";
import { useLoading } from "./context/LoadingContext";
import useAuth from "./hooks/useAuth";
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
    }, [auth]);

    if (!globalStore.isAuthResolved) {
        return <div>{`Loading...`}</div>;
    }

    return (
        <>
            <LoadingOverlay
                visible={loading}
                overlayProps={{
                    opacity: 0.3,
                }}
            />
            <ScrollArea h={"100vh"}>
                <Container p="xl" size={"xl"}>
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
                                path="confirm-email/:userId/:token"
                                element={
                                    <PublicRoute page={<ConfirmEmail />} />
                                }
                            />
                            <Route
                                path="*"
                                element={<Navigate to={"/home"} />}
                            />
                        </Routes>
                    </BrowserRouter>
                </Container>
            </ScrollArea>
        </>
    );
});

export default App;
