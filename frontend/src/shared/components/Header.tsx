import {
    Button,
    Group,
    Menu,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { observer } from "mobx-react";
import { JSX, useState } from "react";
import { CiHome, CiLogout, CiSettings, CiUser } from "react-icons/ci";
import { GoSun } from "react-icons/go";
import { IoMoonOutline } from "react-icons/io5";
import { RxHamburgerMenu } from "react-icons/rx";
import { useNavigate } from "react-router-dom";
import AuthDialog from "../../features/auth/components/AuthDialog";
import useAuth from "../../features/auth/hooks/useAuth";
import globalStore from "../stores/GlobalStore";
import BackButton from "./BackButton";
interface Props {
    color?: string;
    items?: JSX.Element[];
}

const Header = observer((props: Props) => {
    const { colorScheme, toggleColorScheme } = useMantineColorScheme();
    const theme = useMantineTheme();
    const auth = useAuth();
    const navigate = useNavigate();

    const [isOpenAuth, setIsOpenAuth] = useState(false);

    // These are icon buttons at every width, so on a phone they give up the padding a
    // label would have needed. Without it the row they share with a page's own controls
    // is wider than the screen.
    const isMobile = useMediaQuery("(max-width: 48em)");
    const iconButtonPadding = isMobile ? "xs" : undefined;

    return (
        // Never wraps: the header is one row, and a second one would push the page's
        // content down rather than admit the row is too full.
        <Group align="center" justify="flex-end" wrap="nowrap" flex="0 0 auto">
            <Group gap={isMobile ? "xs" : "md"} wrap="nowrap">
                {globalStore.currentUser && <BackButton color={props.color} />}
                <Button
                    variant="outline"
                    px={iconButtonPadding}
                    color={props.color ?? theme.primaryColor}
                    onClick={() => toggleColorScheme()}
                >
                    {colorScheme === "light" ? (
                        <IoMoonOutline size={16} />
                    ) : (
                        <GoSun size={16} />
                    )}
                </Button>
                <Menu zIndex={400}>
                    <Menu.Target>
                        <Button
                            variant="outline"
                            px={iconButtonPadding}
                            color={props.color ?? theme.primaryColor}
                        >
                            <RxHamburgerMenu size={16} />
                        </Button>
                    </Menu.Target>

                    <Menu.Dropdown>
                        {globalStore.currentUser ? (
                            <>
                                <Menu.Item
                                    leftSection={<CiUser size={16} />}
                                    onClick={() => navigate("/profile")}
                                >
                                    Profile
                                </Menu.Item>
                            </>
                        ) : (
                            <Menu.Item
                                leftSection={<CiUser size={16} />}
                                onClick={() => setIsOpenAuth(true)}
                            >
                                {"Login"}
                            </Menu.Item>
                        )}
                        {globalStore.currentUser && (
                            <Menu.Item
                                leftSection={<CiHome size={16} />}
                                onClick={() => navigate("/home")}
                            >
                                Home
                            </Menu.Item>
                        )}
                        {globalStore.userHasRole("admin") && (
                            <Menu.Item
                                leftSection={<CiSettings size={16} />}
                                onClick={() => navigate("/admin-panel")}
                            >
                                Admin panel
                            </Menu.Item>
                        )}
                        {globalStore.currentUser && (
                            <Menu.Item
                                leftSection={<CiLogout size={16} />}
                                onClick={async () => {
                                    await auth.logout();
                                }}
                            >
                                Logout
                            </Menu.Item>
                        )}
                    </Menu.Dropdown>
                </Menu>
            </Group>

            {isOpenAuth && <AuthDialog onClose={() => setIsOpenAuth(false)} />}
        </Group>
    );
});

export default Header;
