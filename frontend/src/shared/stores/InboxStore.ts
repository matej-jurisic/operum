import { makeAutoObservable, runInAction } from "mobx";
import { inboxController } from "../../features/inbox/api/inboxController";
import { InboxNotificationDto } from "../../features/inbox/types/InboxNotificationDto";

const PAGE_SIZE = 15;

/**
 * Backs the sidebar notification bell: the unread badge (polled while the app is
 * open) and the modal list (loaded on open, paged with "Load more"). App-wide
 * rather than tracker-scoped, so it lives beside GlobalStore instead of in a context.
 */
class InboxStore {
    items: InboxNotificationDto[] = [];
    unreadCount = 0;
    loading = false;
    hasMore = false;
    loaded = false;

    constructor() {
        makeAutoObservable(this);
    }

    async refreshUnreadCount() {
        try {
            const res = await inboxController.getUnreadCount();
            runInAction(() => {
                this.unreadCount = res.data;
            });
        } catch {
            // Poll failure is non-critical; keep the last known count.
        }
    }

    async loadFirstPage() {
        this.loading = true;
        try {
            const res = await inboxController.getInbox(0, PAGE_SIZE);
            runInAction(() => {
                this.items = res.data.items;
                this.unreadCount = res.data.unreadCount;
                this.hasMore = res.data.hasMore;
                this.loaded = true;
            });
        } finally {
            runInAction(() => {
                this.loading = false;
            });
        }
    }

    async loadMore() {
        if (this.loading || !this.hasMore) return;
        this.loading = true;
        try {
            const res = await inboxController.getInbox(this.items.length, PAGE_SIZE);
            runInAction(() => {
                this.items = [...this.items, ...res.data.items];
                this.unreadCount = res.data.unreadCount;
                this.hasMore = res.data.hasMore;
            });
        } finally {
            runInAction(() => {
                this.loading = false;
            });
        }
    }

    async markRead(id: string) {
        const item = this.items.find((i) => i.id === id);
        if (!item || item.readAt) return;

        item.readAt = new Date().toISOString();
        this.unreadCount = Math.max(0, this.unreadCount - 1);

        try {
            await inboxController.markRead(id);
        } catch {
            this.refreshUnreadCount();
        }
    }

    async markAllRead() {
        const now = new Date().toISOString();
        this.items.forEach((i) => {
            if (!i.readAt) i.readAt = now;
        });
        this.unreadCount = 0;

        try {
            await inboxController.markAllRead();
        } catch {
            this.refreshUnreadCount();
        }
    }

    async remove(id: string) {
        const item = this.items.find((i) => i.id === id);
        if (!item) return;

        this.items = this.items.filter((i) => i.id !== id);
        if (!item.readAt) this.unreadCount = Math.max(0, this.unreadCount - 1);

        try {
            await inboxController.deleteItem(id);
        } catch {
            this.loadFirstPage();
        }
    }

    reset() {
        this.items = [];
        this.unreadCount = 0;
        this.hasMore = false;
        this.loaded = false;
        this.loading = false;
    }
}

const inboxStore = new InboxStore();
export default inboxStore;
