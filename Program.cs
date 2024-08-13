#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <netdb.h>

#define MAX_HOSTS 256

// Function to execute a system command and retrieve its output
void execute_command(const char *command, char *result, size_t size) {
    FILE *fp;
    fp = popen(command, "r");
    if (fp == NULL) {
        perror("popen");
        return;
    }

    if (fgets(result, size, fp) == NULL) {
        pclose(fp);
        return;
    }

    pclose(fp);
}

// Function to get the MAC address using arp
void get_mac_address(const char *ip, char *mac, size_t size) {
    char command[128];
    snprintf(command, sizeof(command), "arp -n %s | awk '/ether/ {print $3}'", ip);
    execute_command(command, mac, size);
}

// Function to resolve IP to hostname
void resolve_hostname(const char *ip, char *hostname, size_t size) {
    struct in_addr addr;
    inet_aton(ip, &addr);
    struct hostent *host = gethostbyaddr(&addr, sizeof(addr), AF_INET);

    if (host != NULL) {
        strncpy(hostname, host->h_name, size);
    } else {
        snprintf(hostname, size, "Unknown");
    }
}

// Function to ping an IP address
int ping_host(const char *ip) {
    char command[128];
    char result[128];

    snprintf(command, sizeof(command), "ping -c 1 %s", ip);
    execute_command(command, result, sizeof(result));

    return strstr(result, "1 received") != NULL;
}

// Function to scan a subnet
void scan_subnet(const char *subnet) {
    char ip[16];
    char mac[18];
    char hostname[256];

    for (int i = 1; i <= MAX_HOSTS; i++) {
        snprintf(ip, sizeof(ip), "%s.%d", subnet, i);
        if (ping_host(ip)) {
            get_mac_address(ip, mac, sizeof(mac));
            resolve_hostname(ip, hostname, sizeof(hostname));
            printf("IP: %s, MAC: %s, Hostname: %s\n", ip, mac, hostname);
        }
    }
}

int main(int argc, char *argv) {
    if (argc != 2) {
        fprintf(stderr, "Usage: %s <subnet>\n", argv[0]);
        fprintf(stderr, "Example: %s 192.168.1\n", argv[0]);
        return EXIT_FAILURE;
    }

    printf("Scanning subnet: %s.0/24\n", argv[1]);
    scan_subnet(argv[1]);

    return EXIT_SUCCESS;
}