namespace CodeClash.Infrastructure.Seeding;

/// <summary>
/// Provides per-language wrapper driver templates for seeding existing problems.
/// Each template wraps the user's {{submission}} code with a driver that reads
/// from stdin (JSON line) and writes the result to stdout.
/// </summary>
public static class ProblemTemplateSeeder
{
    /// <summary>
    /// Returns the list of (language, wrapperTemplate, starterCode) tuples for a given problem slug.
    /// Returns null if no templates are defined for this slug (new problems use Admin Panel).
    /// </summary>
    public static List<(string Language, string WrapperTemplate, string StarterCode)>? GetTemplates(string slug)
    {
        return slug switch
        {
            "two-sum" => TwoSum(),
            "palindrome-number" => PalindromeNumber(),
            "valid-parentheses" => ValidParentheses(),
            "longest-substring-without-repeating-characters" => LongestSubstring(),
            "invert-binary-tree" => InvertBinaryTree(),
            _ => null
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Two Sum
    // Input (stdin): nums=2,7,11,15 target=9  (plain text format to match existing test cases)
    // Output: [0,1]
    // ─────────────────────────────────────────────────────────────────────────
    private static List<(string, string, string)> TwoSum() =>
    [
        ("csharp", """
using System;
using System.Text.RegularExpressions;
using System.Linq;

{{submission}}

public class Driver {
    public static void Main() {
        string line = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(line)) return;
        
        int[] nums;
        int target;
        
        if (line.Contains("nums") && line.Contains("target")) {
            var numsMatch = Regex.Match(line, @"nums\s*=\s*\[([^\]]*)\]");
            nums = numsMatch.Success && !string.IsNullOrWhiteSpace(numsMatch.Groups[1].Value)
                ? numsMatch.Groups[1].Value.Split(',').Select(x => int.Parse(x.Trim())).ToArray()
                : new int[0];
            var targetMatch = Regex.Match(line, @"target\s*=\s*(-?\d+)");
            target = targetMatch.Success ? int.Parse(targetMatch.Groups[1].Value) : 0;
        } else {
            string[] parts = line.Trim().Split(' ');
            target = int.Parse(parts[0]);
            nums = new int[parts.Length - 1];
            for (int i = 0; i < nums.Length; i++) nums[i] = int.Parse(parts[i + 1]);
        }
        
        int[] res = new Solution().TwoSum(nums, target);
        Console.WriteLine($"[ {res[0]}, {res[1]} ]");
    }
}
""",
        """
public class Solution {
    public int[] TwoSum(int[] nums, int target) {
        // Your code here
        return new int[] {};
    }
}
"""),

        ("python", """
import sys
import re

{{submission}}

line = sys.stdin.read().strip()
if line:
    if "nums" in line and "target" in line:
        nums_match = re.search(r'nums\s*=\s*\[([^\]]*)\]', line)
        nums = [int(x.strip()) for x in nums_match.group(1).split(',')] if nums_match and nums_match.group(1).strip() else []
        target_match = re.search(r'target\s*=\s*(-?\d+)', line)
        target = int(target_match.group(1)) if target_match else 0
    else:
        parts = line.split()
        target = int(parts[0])
        nums = [int(x) for x in parts[1:]]
        
    res = Solution().twoSum(nums, target)
    print(f"[ {res[0]}, {res[1]} ]")
""",
        """
class Solution:
    def twoSum(self, nums: list[int], target: int) -> list[int]:
        pass
"""),

        ("javascript", """
{{submission}}

const line = require('fs').readFileSync('/dev/stdin','utf-8').trim();
if (line) {
    let nums = [];
    let target = 0;
    
    if (line.includes("nums") && line.includes("target")) {
        const numsMatch = line.match(/nums\s*=\s*\[([^\]]*)\]/);
        nums = numsMatch && numsMatch[1].trim() ? numsMatch[1].split(',').map(Number) : [];
        const targetMatch = line.match(/target\s*=\s*(-?\d+)/);
        target = targetMatch ? parseInt(targetMatch[1]) : 0;
    } else {
        const parts = line.split(' ');
        target = parseInt(parts[0]);
        nums = parts.slice(1).map(Number);
    }
    
    const res = twoSum(nums, target);
    console.log(`[ ${res[0]}, ${res[1]} ]`);
}
""",
        """
/**
 * @param {number[]} nums
 * @param {number} target
 * @return {number[]}
 */
var twoSum = function(nums, target) {

};
"""),

        ("java", """
{{submission}}

import java.util.Scanner;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.ArrayList;

public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        if (sc.hasNextLine()) {
            String line = sc.nextLine().trim();
            int[] nums;
            int target;
            
            if (line.contains("nums") && line.contains("target")) {
                Pattern numsPat = Pattern.compile("nums\\s*=\\s*\\[([^\\]]*)\\]");
                Matcher numsMat = numsPat.matcher(line);
                if (numsMat.find() && !numsMat.group(1).trim().isEmpty()) {
                    String[] parts = numsMat.group(1).split(",");
                    nums = new int[parts.length];
                    for (int i = 0; i < parts.length; i++) {
                        nums[i] = Integer.parseInt(parts[i].trim());
                    }
                } else {
                    nums = new int[0];
                }
                
                Pattern targetPat = Pattern.compile("target\\s*=\\s*(-?\\d+)");
                Matcher targetMat = targetPat.matcher(line);
                target = targetMat.find() ? Integer.parseInt(targetMat.group(1)) : 0;
            } else {
                String[] parts = line.split(" ");
                target = Integer.parseInt(parts[0]);
                nums = new int[parts.length - 1];
                for (int i = 0; i < nums.length; i++) {
                    nums[i] = Integer.parseInt(parts[i+1]);
                }
            }
            
            int[] res = new Solution().twoSum(nums, target);
            System.out.println("[ " + res[0] + ", " + res[1] + " ]");
        }
    }
}
""",
        """
class Solution {
    public int[] twoSum(int[] nums, int target) {
        // Your code here
        return new int[]{};
    }
}
"""),

        ("cpp", """
#include <iostream>
#include <vector>
#include <string>
#include <regex>
#include <sstream>
using namespace std;

{{submission}}

int main() {
    string line;
    if (getline(cin, line)) {
        vector<int> nums;
        int target = 0;
        
        if (line.find("nums") != string::npos && line.find("target") != string::npos) {
            smatch m;
            if (regex_search(line, m, regex("nums\\s*=\\s*\\[([^\\]]*)\\]"))) {
                string numsStr = m[1].str();
                stringstream ss(numsStr);
                string item;
                while (getline(ss, item, ',')) {
                    if (!item.empty()) {
                        nums.push_back(stoi(item));
                    }
                }
            }
            if (regex_search(line, m, regex("target\\s*=\\s*(-?\\d+)"))) {
                target = stoi(m[1].str());
            }
        } else {
            istringstream iss(line);
            iss >> target;
            int x;
            while (iss >> x) nums.push_back(x);
        }
        
        Solution sol;
        vector<int> res = sol.twoSum(nums, target);
        cout << "[ " << res[0] << ", " << res[1] << " ]" << endl;
    }
    return 0;
}
""",
        """
class Solution {
public:
    vector<int> twoSum(vector<int>& nums, int target) {
        // Your code here
        return {};
    }
};
"""),
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Palindrome Number
    // Input (stdin): single integer
    // Output: true / false
    // ─────────────────────────────────────────────────────────────────────────
    private static List<(string, string, string)> PalindromeNumber() =>
    [
        ("csharp", """
using System;

{{submission}}

public class Driver {
    public static void Main() {
        string line = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(line)) return;
        int x = int.Parse(line);
        bool res = new Solution().IsPalindrome(x);
        Console.WriteLine(res.ToString().ToLower());
    }
}
""",
        """
public class Solution {
    public bool IsPalindrome(int x) {
        
    }
}
"""),

        ("python", """
import sys

{{submission}}

line = sys.stdin.read().strip()
if line:
    x = int(line)
    res = Solution().isPalindrome(x)
    print(str(res).lower())
""",
        """
class Solution:
    def isPalindrome(self, x: int) -> bool:
        pass
"""),

        ("javascript", """
{{submission}}

const input = require('fs').readFileSync('/dev/stdin','utf-8').trim();
if (input) {
    const x = parseInt(input, 10);
    const res = isPalindrome(x);
    console.log(res.toString());
}
""",
        """
/**
 * @param {number} x
 * @return {boolean}
 */
var isPalindrome = function(x) {

};
"""),

        ("java", """
{{submission}}

import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        if (sc.hasNextInt()) {
            int x = sc.nextInt();
            boolean res = new Solution().isPalindrome(x);
            System.out.println(res);
        }
    }
}
""",
        """
class Solution {
    public boolean isPalindrome(int x) {
        
    }
}
"""),

        ("cpp", """
#include <iostream>
using namespace std;

{{submission}}

int main() {
    int x;
    if (cin >> x) {
        Solution sol;
        bool res = sol.isPalindrome(x);
        cout << (res ? "true" : "false") << endl;
    }
    return 0;
}
""",
        """
class Solution {
public:
    bool isPalindrome(int x) {
        
    }
};
"""),
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Valid Parentheses
    // Input (stdin): the string s (unquoted)
    // Output: true / false
    // ─────────────────────────────────────────────────────────────────────────
    private static List<(string, string, string)> ValidParentheses() =>
    [
        ("csharp", """
using System;

{{submission}}

public class Driver {
    public static void Main() {
        string s = Console.ReadLine() ?? "";
        bool res = new Solution().IsValid(s);
        Console.WriteLine(res.ToString().ToLower());
    }
}
""",
        """
public class Solution {
    public bool IsValid(string s) {
        
    }
}
"""),

        ("python", """
import sys

{{submission}}

s = sys.stdin.read().strip()
res = Solution().isValid(s)
print(str(res).lower())
""",
        """
class Solution:
    def isValid(self, s: str) -> bool:
        pass
"""),

        ("javascript", """
{{submission}}

const input = require('fs').readFileSync('/dev/stdin','utf-8').trim();
const res = isValid(input);
console.log(res.toString());
""",
        """
/**
 * @param {string} s
 * @return {boolean}
 */
var isValid = function(s) {

};
"""),

        ("java", """
{{submission}}

import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        String s = sc.hasNextLine() ? sc.nextLine() : "";
        boolean res = new Solution().isValid(s);
        System.out.println(res);
    }
}
""",
        """
class Solution {
    public boolean isValid(String s) {
        
    }
}
"""),

        ("cpp", """
#include <iostream>
#include <string>
using namespace std;

{{submission}}

int main() {
    string s;
    if (getline(cin, s)) {
        Solution sol;
        bool res = sol.isValid(s);
        cout << (res ? "true" : "false") << endl;
    }
    return 0;
}
""",
        """
class Solution {
public:
    bool isValid(string s) {
        
    }
};
"""),
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Longest Substring Without Repeating Characters
    // Input (stdin): the string s (unquoted)
    // Output: integer
    // ─────────────────────────────────────────────────────────────────────────
    private static List<(string, string, string)> LongestSubstring() =>
    [
        ("csharp", """
using System;

{{submission}}

public class Driver {
    public static void Main() {
        string s = Console.ReadLine() ?? "";
        int res = new Solution().LengthOfLongestSubstring(s);
        Console.WriteLine(res);
    }
}
""",
        """
public class Solution {
    public int LengthOfLongestSubstring(string s) {
        
    }
}
"""),

        ("python", """
import sys

{{submission}}

s = sys.stdin.read().strip()
res = Solution().lengthOfLongestSubstring(s)
print(res)
""",
        """
class Solution:
    def lengthOfLongestSubstring(self, s: str) -> int:
        pass
"""),

        ("javascript", """
{{submission}}

const input = require('fs').readFileSync('/dev/stdin','utf-8').trim();
const res = lengthOfLongestSubstring(input);
console.log(res);
""",
        """
/**
 * @param {string} s
 * @return {number}
 */
var lengthOfLongestSubstring = function(s) {

};
"""),

        ("java", """
{{submission}}

import java.util.Scanner;
public class Main {
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        String s = sc.hasNextLine() ? sc.nextLine() : "";
        int res = new Solution().lengthOfLongestSubstring(s);
        System.out.println(res);
    }
}
""",
        """
class Solution {
    public int lengthOfLongestSubstring(String s) {
        
    }
}
"""),

        ("cpp", """
#include <iostream>
#include <string>
using namespace std;

{{submission}}

int main() {
    string s;
    if (getline(cin, s)) {
        Solution sol;
        int res = sol.lengthOfLongestSubstring(s);
        cout << res << endl;
    }
    return 0;
}
""",
        """
class Solution {
public:
    int lengthOfLongestSubstring(string s) {
        
    }
};
"""),
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Invert Binary Tree
    // Input (stdin): BFS array like [4,2,7,1,3,6,9]
    // Output: BFS array like [7,4,2,9,6,3,1]
    // ─────────────────────────────────────────────────────────────────────────
    private static List<(string, string, string)> InvertBinaryTree() =>
    [
        ("csharp", """
using System;
using System.Collections.Generic;

public class TreeNode {
    public int val;
    public TreeNode left, right;
    public TreeNode(int val = 0, TreeNode left = null, TreeNode right = null) {
        this.val = val; this.left = left; this.right = right;
    }
}

{{submission}}

public class Driver {
    public static TreeNode Deserialize(string data) {
        data = data.Trim().Trim('[', ']');
        if (string.IsNullOrEmpty(data)) return null;
        string[] parts = data.Split(',');
        if (parts.Length == 0 || parts[0].Trim() == "null") return null;
        TreeNode root = new TreeNode(int.Parse(parts[0].Trim()));
        Queue<TreeNode> q = new Queue<TreeNode>();
        q.Enqueue(root);
        int i = 1;
        while (q.Count > 0 && i < parts.Length) {
            TreeNode cur = q.Dequeue();
            if (i < parts.Length && parts[i].Trim() != "null") {
                cur.left = new TreeNode(int.Parse(parts[i].Trim())); q.Enqueue(cur.left);
            }
            i++;
            if (i < parts.Length && parts[i].Trim() != "null") {
                cur.right = new TreeNode(int.Parse(parts[i].Trim())); q.Enqueue(cur.right);
            }
            i++;
        }
        return root;
    }
    public static string Serialize(TreeNode root) {
        if (root == null) return "[]";
        var res = new List<string>();
        var q = new Queue<TreeNode>();
        q.Enqueue(root);
        while (q.Count > 0) {
            var node = q.Dequeue();
            if (node != null) { res.Add(node.val.ToString()); q.Enqueue(node.left); q.Enqueue(node.right); }
            else res.Add("null");
        }
        while (res.Count > 0 && res[res.Count-1] == "null") res.RemoveAt(res.Count-1);
        return "[" + string.Join(",", res) + "]";
    }
    public static void Main() {
        string line = Console.ReadLine() ?? "";
        TreeNode root = Deserialize(line);
        TreeNode result = new Solution().InvertTree(root);
        Console.WriteLine(Serialize(result));
    }
}
""",
        """
public class Solution {
    public TreeNode InvertTree(TreeNode root) {
        
    }
}
"""),

        ("python", """
import sys
from collections import deque

class TreeNode:
    def __init__(self, val=0, left=None, right=None):
        self.val = val
        self.left = left
        self.right = right

def deserialize(data):
    data = data.strip().strip('[]')
    if not data: return None
    parts = [x.strip() for x in data.split(',')]
    if not parts or parts[0] == 'null': return None
    root = TreeNode(int(parts[0]))
    q = deque([root])
    i = 1
    while q and i < len(parts):
        node = q.popleft()
        if i < len(parts) and parts[i] != 'null':
            node.left = TreeNode(int(parts[i])); q.append(node.left)
        i += 1
        if i < len(parts) and parts[i] != 'null':
            node.right = TreeNode(int(parts[i])); q.append(node.right)
        i += 1
    return root

def serialize(root):
    if not root: return "[]"
    res, q = [], deque([root])
    while q:
        node = q.popleft()
        if node: res.append(str(node.val)); q.append(node.left); q.append(node.right)
        else: res.append("null")
    while res and res[-1] == "null": res.pop()
    return "[" + ",".join(res) + "]"

{{submission}}

line = sys.stdin.read().strip()
if line:
    root = deserialize(line)
    sol = Solution()
    print(serialize(sol.invertTree(root)))
""",
        """
class Solution:
    def invertTree(self, root):
        pass
"""),

        ("javascript", """
function TreeNode(val, left, right) {
    this.val = val ?? 0;
    this.left = left ?? null;
    this.right = right ?? null;
}

function deserialize(data) {
    data = data.trim().replace(/^\[|\]$/g,'');
    if (!data) return null;
    const parts = data.split(',').map(s => s.trim());
    if (!parts.length || parts[0] === 'null') return null;
    const root = new TreeNode(parseInt(parts[0]));
    const q = [root];
    let i = 1;
    while (q.length && i < parts.length) {
        const node = q.shift();
        if (i < parts.length && parts[i] !== 'null') { node.left = new TreeNode(parseInt(parts[i])); q.push(node.left); } i++;
        if (i < parts.length && parts[i] !== 'null') { node.right = new TreeNode(parseInt(parts[i])); q.push(node.right); } i++;
    }
    return root;
}

function serialize(root) {
    if (!root) return "[]";
    const res = [], q = [root];
    while (q.length) {
        const node = q.shift();
        if (node) { res.push(node.val.toString()); q.push(node.left); q.push(node.right); }
        else res.push("null");
    }
    while (res.length && res[res.length-1] === "null") res.pop();
    return "[" + res.join(",") + "]";
}

{{submission}}

const input = require('fs').readFileSync('/dev/stdin','utf-8').trim();
if (input) {
    const root = deserialize(input);
    const result = invertTree(root);
    console.log(serialize(result));
}
""",
        """
/**
 * @param {TreeNode} root
 * @return {TreeNode}
 */
var invertTree = function(root) {

};
"""),

        ("java", """
import java.util.*;

class TreeNode {
    int val;
    TreeNode left, right;
    TreeNode(int val) { this.val = val; }
}

{{submission}}

public class Main {
    static TreeNode deserialize(String data) {
        data = data.trim().replaceAll("[\\[\\]]","");
        if (data.isEmpty()) return null;
        String[] parts = data.split(",");
        if (parts[0].trim().equals("null")) return null;
        TreeNode root = new TreeNode(Integer.parseInt(parts[0].trim()));
        Queue<TreeNode> q = new LinkedList<>();
        q.offer(root);
        int i = 1;
        while (!q.isEmpty() && i < parts.length) {
            TreeNode cur = q.poll();
            if (i < parts.length && !parts[i].trim().equals("null")) {
                cur.left = new TreeNode(Integer.parseInt(parts[i].trim())); q.offer(cur.left);
            }
            i++;
            if (i < parts.length && !parts[i].trim().equals("null")) {
                cur.right = new TreeNode(Integer.parseInt(parts[i].trim())); q.offer(cur.right);
            }
            i++;
        }
        return root;
    }
    static String serialize(TreeNode root) {
        if (root == null) return "[]";
        List<String> res = new ArrayList<>();
        Queue<TreeNode> q = new LinkedList<>();
        q.offer(root);
        while (!q.isEmpty()) {
            TreeNode node = q.poll();
            if (node != null) { res.add(String.valueOf(node.val)); q.offer(node.left); q.offer(node.right); }
            else res.add("null");
        }
        while (!res.isEmpty() && res.get(res.size()-1).equals("null")) res.remove(res.size()-1);
        return "[" + String.join(",", res) + "]";
    }
    public static void main(String[] args) {
        Scanner sc = new Scanner(System.in);
        if (sc.hasNextLine()) {
            TreeNode root = deserialize(sc.nextLine());
            TreeNode result = new Solution().invertTree(root);
            System.out.println(serialize(result));
        }
    }
}
""",
        """
class Solution {
    public TreeNode invertTree(TreeNode root) {
        
    }
}
"""),

        ("cpp", """
#include <iostream>
#include <string>
#include <vector>
#include <queue>
#include <sstream>
#include <algorithm>
using namespace std;

struct TreeNode {
    int val;
    TreeNode *left, *right;
    TreeNode(int x) : val(x), left(nullptr), right(nullptr) {}
};

{{submission}}

TreeNode* deserialize(string data) {
    data.erase(remove(data.begin(), data.end(), '['), data.end());
    data.erase(remove(data.begin(), data.end(), ']'), data.end());
    if (data.empty()) return nullptr;
    istringstream iss(data);
    string item;
    vector<string> parts;
    while (getline(iss, item, ',')) parts.push_back(item);
    if (parts.empty() || parts[0] == "null") return nullptr;
    TreeNode* root = new TreeNode(stoi(parts[0]));
    queue<TreeNode*> q;
    q.push(root);
    int i = 1;
    while (!q.empty() && i < (int)parts.size()) {
        TreeNode* cur = q.front(); q.pop();
        if (i < (int)parts.size() && parts[i] != "null") { cur->left = new TreeNode(stoi(parts[i])); q.push(cur->left); } i++;
        if (i < (int)parts.size() && parts[i] != "null") { cur->right = new TreeNode(stoi(parts[i])); q.push(cur->right); } i++;
    }
    return root;
}

string serialize(TreeNode* root) {
    if (!root) return "[]";
    vector<string> res;
    queue<TreeNode*> q;
    q.push(root);
    while (!q.empty()) {
        TreeNode* node = q.front(); q.pop();
        if (node) { res.push_back(to_string(node->val)); q.push(node->left); q.push(node->right); }
        else res.push_back("null");
    }
    while (!res.empty() && res.back() == "null") res.pop_back();
    string out = "[";
    for (int i = 0; i < (int)res.size(); i++) { if (i) out += ","; out += res[i]; }
    return out + "]";
}

int main() {
    string line;
    if (getline(cin, line)) {
        TreeNode* root = deserialize(line);
        Solution sol;
        TreeNode* result = sol.invertTree(root);
        cout << serialize(result) << endl;
    }
    return 0;
}
""",
        """
class Solution {
public:
    TreeNode* invertTree(TreeNode* root) {
        
    }
};
"""),
    ];
}
