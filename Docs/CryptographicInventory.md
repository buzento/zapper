# Cryptographic inventory

| Key, algorithm or certificate | Where it may be used | Where it may not be used | What it may protect | What it may not protect |
|---|---|---|---|---|
| The card token key, `Zapper:TokenEncryption:Key`: 32 random bytes, used with AES-256-GCM, a 12-byte random nonce and a 16-byte tag, the stored card's customer reference as the associated data | Only by `CryptographyManager`, in the hosts that encrypt or decrypt a card token | Anywhere else, and never to sign, to hash or to protect any other value | Only `Customer.StoredCard.EncryptedToken` | Any other column, file, message or secret |
