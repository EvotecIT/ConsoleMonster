/* The CLR never runs between fork and exec. posix_spawn starts this standalone
 * process with PTY stdio and a startup-status pipe on descriptor 3. */
#include <errno.h>
#include <fcntl.h>
#include <signal.h>
#include <sys/ioctl.h>
#include <unistd.h>

static void fail(void) {
    int error = errno;
    const char *next = (const char *)&error;
    size_t left = sizeof(error);
    while (left) {
        ssize_t count = write(3, next, left);
        if (count < 0 && errno == EINTR) continue;
        if (count <= 0) break;
        next += count;
        left -= (size_t)count;
    }
    _exit(127);
}

int main(int argc, char **argv) {
    if (argc < 2) { errno = EINVAL; fail(); }
    if (fcntl(3, F_SETFD, FD_CLOEXEC) < 0) fail();
    if (setsid() < 0 || ioctl(0, TIOCSCTTY, 0) < 0) fail();
    sigset_t mask;
    sigemptyset(&mask);
    if (sigprocmask(SIG_SETMASK, &mask, NULL) < 0) fail();
    const int signals[] = { SIGINT, SIGQUIT, SIGTERM, SIGHUP, SIGWINCH, SIGTSTP, SIGTTIN, SIGTTOU };
    for (size_t i = 0; i < sizeof(signals) / sizeof(signals[0]); ++i) {
        if (signal(signals[i], SIG_DFL) == SIG_ERR) fail();
    }
    execvp(argv[1], argv + 1);
    fail();
}
