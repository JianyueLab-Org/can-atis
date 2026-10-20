"""Python 3.12 compatibility patch for pymumble's removed ssl.wrap_socket."""
import os
import select
import socket
import ssl
import time


def install():
    if hasattr(ssl, "wrap_socket"):
        return

    def wrap_socket(sock, keyfile=None, certfile=None, server_side=False,
                    cert_reqs=ssl.CERT_NONE, ssl_version=ssl.PROTOCOL_TLS,
                    ca_certs=None, do_handshake_on_connect=True,
                    suppress_ragged_eofs=True, ciphers=None):
        context = ssl.SSLContext(ssl_version)
        context.check_hostname = False
        context.verify_mode = cert_reqs
        if ciphers:
            context.set_ciphers(ciphers)
        if certfile:
            context.load_cert_chain(certfile, keyfile)
        if ca_certs:
            context.load_verify_locations(ca_certs)
        return context.wrap_socket(sock, server_side=server_side,
                                   do_handshake_on_connect=do_handshake_on_connect,
                                   suppress_ragged_eofs=suppress_ragged_eofs)

    ssl.wrap_socket = wrap_socket
