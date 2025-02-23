import { useEffect } from "react";
import api from "./api/Api";

function App() {
    useEffect(() => {
        api.get("/projects");
    }, []);

    return <></>;
}

export default App;
