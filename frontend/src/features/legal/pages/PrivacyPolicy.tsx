import {
    Anchor,
    Container,
    Divider,
    Group,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useNavigate } from "react-router-dom";

function Section({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <Stack gap="xs">
            <Title order={3}>{title}</Title>
            {children}
        </Stack>
    );
}

export default function PrivacyPolicy() {
    const theme = useMantineTheme();
    const navigate = useNavigate();

    return (
        <Container size="md" py="xl">
            <Stack gap="xl">
                <Stack gap={4}>
                    <Anchor
                        component="button"
                        onClick={() => navigate(-1)}
                        c="dimmed"
                        size="sm"
                    >
                        ← Back
                    </Anchor>
                    <Title order={1} c={theme.primaryColor}>Privacy Policy</Title>
                    <Text size="sm" c="dimmed">Last updated: April 28, 2026</Text>
                </Stack>

                <Text c="dimmed">
                    This Privacy Policy explains how Operum ("we", "us", or "our") collects,
                    uses, and protects your personal information when you use operum.app.
                </Text>

                <Divider />

                <Section title="1. Information We Collect">
                    <Text c="dimmed">We collect the following information when you use Operum:</Text>
                    <Stack gap={4} pl="md">
                        <Text c="dimmed">— <strong>Account information:</strong> your email address and username when you register.</Text>
                        <Text c="dimmed">— <strong>User content:</strong> tracker schemas, field definitions, and entries you create inside the app.</Text>
                        <Text c="dimmed">— <strong>Authentication data:</strong> securely hashed passwords and short-lived authentication tokens stored in HTTP-only cookies.</Text>
                        <Text c="dimmed">— <strong>Usage data:</strong> basic server logs (IP address, timestamps) for security and debugging purposes.</Text>
                    </Stack>
                    <Text c="dimmed">
                        If you register via Google, we receive your verified email address and name
                        from Google. We do not receive your Google password.
                    </Text>
                </Section>

                <Section title="2. How We Use Your Information">
                    <Stack gap={4} pl="md">
                        <Text c="dimmed">— To provide, operate, and maintain the Operum service.</Text>
                        <Text c="dimmed">— To send you account-related emails (email confirmation, password reset).</Text>
                        <Text c="dimmed">— To identify and fix bugs and security issues.</Text>
                        <Text c="dimmed">— To comply with legal obligations.</Text>
                    </Stack>
                    <Text c="dimmed">
                        We do not sell your personal data. We do not use your data for advertising.
                    </Text>
                </Section>

                <Section title="3. Third-Party Services">
                    <Text c="dimmed">We use the following third-party services to operate Operum:</Text>
                    <Stack gap={4} pl="md">
                        <Text c="dimmed">— <strong>Mailgun</strong> — for sending transactional emails (email confirmation). Your email address is shared with Mailgun solely to deliver these messages.</Text>
                        <Text c="dimmed">— <strong>Google OAuth</strong> — if you choose to sign in with Google. Governed by <Anchor href="https://policies.google.com/privacy" target="_blank" size="sm">Google's Privacy Policy</Anchor>.</Text>
                    </Stack>
                </Section>

                <Section title="4. Data Retention and Deletion">
                    <Text c="dimmed">
                        Your data is retained for as long as your account is active. You can permanently
                        delete your account and all associated data at any time from the Profile page.
                        Deletion is immediate and irreversible.
                    </Text>
                </Section>

                <Section title="5. Cookies">
                    <Text c="dimmed">
                        Operum uses HTTP-only cookies strictly for authentication purposes (storing your
                        session token and refresh token). These are functional cookies required to keep
                        you logged in. We do not use tracking cookies or advertising cookies.
                    </Text>
                </Section>

                <Section title="6. Data Security">
                    <Text c="dimmed">
                        We take reasonable technical measures to protect your data, including encrypted
                        connections (HTTPS), HTTP-only authentication cookies, and hashed password storage.
                        No method of transmission over the internet is 100% secure, and we cannot
                        guarantee absolute security.
                    </Text>
                </Section>

                <Section title="7. Your Rights">
                    <Text c="dimmed">
                        Depending on your location, you may have rights to access, correct, or delete your
                        personal data. You can update your username directly in the app and delete your
                        account at any time. For other requests, contact us at the address below.
                    </Text>
                </Section>

                <Section title="8. Changes to This Policy">
                    <Text c="dimmed">
                        We may update this Privacy Policy from time to time. We will notify you of significant
                        changes by updating the date at the top of this page. Continued use of Operum after
                        changes constitutes acceptance of the updated policy.
                    </Text>
                </Section>

                <Section title="9. Contact">
                    <Text c="dimmed">
                        If you have questions about this Privacy Policy, please contact us at{" "}
                        <Anchor href="mailto:mjurisic812@gmail.com">mjurisic812@gmail.com</Anchor>.
                    </Text>
                </Section>

                <Divider />

                <Group gap="xl">
                    <Anchor
                        component="button"
                        onClick={() => navigate("/terms")}
                        size="sm"
                        c="dimmed"
                    >
                        Terms of Service →
                    </Anchor>
                </Group>
            </Stack>
        </Container>
    );
}
