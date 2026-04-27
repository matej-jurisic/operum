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

export default function TermsOfService() {
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
                    <Title order={1} c={theme.primaryColor}>Terms of Service</Title>
                    <Text size="sm" c="dimmed">Last updated: April 28, 2026</Text>
                </Stack>

                <Text c="dimmed">
                    These Terms of Service ("Terms") govern your access to and use of Operum ("the
                    Service"), operated at operum.app. By creating an account or using the Service,
                    you agree to these Terms.
                </Text>

                <Divider />

                <Section title="1. Acceptance of Terms">
                    <Text c="dimmed">
                        By registering for or using Operum, you confirm that you are at least 16 years
                        old and that you agree to these Terms. If you do not agree, do not use the Service.
                    </Text>
                </Section>

                <Section title="2. Your Account">
                    <Text c="dimmed">
                        You are responsible for maintaining the confidentiality of your account credentials
                        and for all activity that occurs under your account. You agree to notify us
                        immediately of any unauthorized use of your account.
                    </Text>
                    <Text c="dimmed">
                        You must provide accurate information when registering. Each account is for a single
                        user — account sharing is not permitted.
                    </Text>
                </Section>

                <Section title="3. Your Data">
                    <Text c="dimmed">
                        You retain full ownership of all data you enter into Operum — your trackers, fields,
                        entries, and any other content you create. We do not claim any rights to your data.
                    </Text>
                    <Text c="dimmed">
                        By using Operum, you grant us a limited license to store and process your data
                        solely for the purpose of providing the Service to you.
                    </Text>
                </Section>

                <Section title="4. Acceptable Use">
                    <Text c="dimmed">You agree not to:</Text>
                    <Stack gap={4} pl="md">
                        <Text c="dimmed">— Use the Service for any unlawful purpose.</Text>
                        <Text c="dimmed">— Attempt to gain unauthorized access to other users' accounts or data.</Text>
                        <Text c="dimmed">— Interfere with or disrupt the Service or servers.</Text>
                        <Text c="dimmed">— Use the Service to store or distribute malicious code.</Text>
                        <Text c="dimmed">— Attempt to reverse-engineer or extract the source code of the Service.</Text>
                    </Stack>
                </Section>

                <Section title="5. Service Availability">
                    <Text c="dimmed">
                        We aim to keep Operum available at all times but make no guarantees of uptime.
                        The Service may be interrupted for maintenance, updates, or reasons beyond our
                        control. We are not liable for any loss resulting from downtime.
                    </Text>
                </Section>

                <Section title="6. Termination">
                    <Text c="dimmed">
                        You may delete your account at any time from the Profile page, which permanently
                        removes your data. We reserve the right to suspend or terminate accounts that
                        violate these Terms, with or without notice.
                    </Text>
                </Section>

                <Section title="7. Disclaimer of Warranties">
                    <Text c="dimmed">
                        Operum is provided "as is" and "as available" without warranties of any kind,
                        either express or implied. We do not warrant that the Service will be error-free,
                        secure, or uninterrupted.
                    </Text>
                </Section>

                <Section title="8. Limitation of Liability">
                    <Text c="dimmed">
                        To the fullest extent permitted by law, Operum and its operators shall not be
                        liable for any indirect, incidental, special, or consequential damages arising
                        from your use of or inability to use the Service, including any loss of data.
                    </Text>
                </Section>

                <Section title="9. Changes to These Terms">
                    <Text c="dimmed">
                        We may update these Terms from time to time. We will notify you of significant
                        changes by updating the date at the top of this page. Continued use of the Service
                        after changes constitutes acceptance of the updated Terms.
                    </Text>
                </Section>

                <Section title="10. Contact">
                    <Text c="dimmed">
                        If you have questions about these Terms, please contact us at{" "}
                        <Anchor href="mailto:mjurisic812@gmail.com">mjurisic812@gmail.com</Anchor>.
                    </Text>
                </Section>

                <Divider />

                <Group gap="xl">
                    <Anchor
                        component="button"
                        onClick={() => navigate("/privacy")}
                        size="sm"
                        c="dimmed"
                    >
                        Privacy Policy →
                    </Anchor>
                </Group>
            </Stack>
        </Container>
    );
}
