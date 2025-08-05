import { Container } from "@mantine/core";
import { observer } from "mobx-react";
import { useEffect } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import "./App.css";
import { PrivateRoute } from "./components/routing/PrivateRoute";
import PublicRoute from "./components/routing/PublicRoute";
import useAuth from "./hooks/useAuth";
import Auth from "./pages/Auth";
import Home from "./pages/Home";
import Tracker from "./pages/Tracker";
import globalStore from "./stores/GlobalStore";

const App = observer(() => {
    const auth = useAuth();

    useEffect(() => {
        auth.handleUserLoggedInCheck();
    }, [auth]);

    if (!globalStore.isAuthResolved) {
        return <div>{`Loading...`}</div>;
    }

    return (
        <>
            <Container size="lg" pt="xl">
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
                        <Route path="*" element={<Navigate to={"/home"} />} />
                    </Routes>
                </BrowserRouter>
            </Container>
        </>
    );
});

export default App;
